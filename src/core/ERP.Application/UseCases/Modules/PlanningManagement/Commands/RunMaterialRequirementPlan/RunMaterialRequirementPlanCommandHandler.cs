using ERP.Application.UseCases.Modules.PlanningManagement.DTOs;
using ERP.Application.UseCases.Modules.PlanningManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PlanningManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PlanningManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Application.UseCases.Modules.PlanningManagement.Commands.RunMaterialRequirementPlan
{
    /// <summary>
    /// Runs material requirements planning over a horizon and writes what it found.
    /// <para>
    /// Demand, supply and stock are read once at the start, not per item. Planning takes a
    /// photograph of the position at a moment; if the numbers moved underneath the run, its
    /// proposals would stop agreeing with each other and the buyer would be ordering against one
    /// picture while the scheduler worked from another.
    /// </para>
    /// </summary>
    public class RunMaterialRequirementPlanCommandHandler(
        IDemandForecastRepository forecasts,
        IItemRepository items,
        IItemInventoryRepository inventories,
        IPurchaseOrderRepository purchaseOrders,
        IProductionOrderRepository productionOrders,
        IMaterialRequirementPlanRepository plans,
        INumberingSeriesRepository numberingSeries,
        TimeProvider timeProvider)
        : IRequestHandler<RunMaterialRequirementPlanCommand, Result<MaterialRequirementPlanDto, Error>>
    {
        private const string PlanNumberingKey = "MaterialRequirementPlan";

        public async Task<Result<MaterialRequirementPlanDto, Error>> Handle(
            RunMaterialRequirementPlanCommand request,
            CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var horizon = DateRange.Create(request.HorizonStart, request.HorizonEnd);

            DemandForecast? forecast = null;
            if (request.DemandForecastId is not null)
            {
                forecast = await forecasts.GetWithLinesAsync(request.DemandForecastId.Value, cancellationToken);
                if (forecast is null)
                    return Result.Failure<MaterialRequirementPlanDto, Error>(
                        Errors.General.NotFound(request.DemandForecastId.Value.ToString(), nameof(DemandForecast)));

                // A draft forecast is a working document. Planning against one commits money to
                // numbers nobody has agreed yet.
                if (!forecast.IsUsable)
                    return Result.Failure<MaterialRequirementPlanDto, Error>(Errors.Planning.ForecastIsNotApproved(forecast.Code));
            }

            var scope = await ResolveScopeAsync(request, cancellationToken);
            if (scope.IsFailure)
                return Result.Failure<MaterialRequirementPlanDto, Error>(scope.Error);

            if (scope.Value.Count == 0)
                return Result.Failure<MaterialRequirementPlanDto, Error>(Errors.Planning.NothingToPlan());

            var series = await numberingSeries.GetForUpdateAsync(PlanNumberingKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<MaterialRequirementPlanDto, Error>(Errors.Order.NumberingSeriesMissing(PlanNumberingKey));

            var plan = new MaterialRequirementPlan(series.Next(today), horizon, runBy: null);

            if (forecast is not null)
                plan.BaseOnForecast(forecast.Id);

            if (request.WarehouseId is not null)
                plan.ScopeToWarehouse(request.WarehouseId.Value);

            if (request.BranchId is not null)
                plan.AssignToBranch(request.BranchId.Value);

            plan.SetDescription(request.Description);

            foreach (var item in scope.Value)
            {
                var line = await PlanItemAsync(plan, item, forecast, request, cancellationToken);
                if (line.IsFailure)
                    return Result.Failure<MaterialRequirementPlanDto, Error>(line.Error);

                plan.AddLine(line.Value);
            }

            plan.Complete();

            await plans.CreateAsync(plan, cancellationToken);

            return Result.Success<MaterialRequirementPlanDto, Error>(plan.ToDto());
        }

        private async Task<Result<List<Item>, Error>> ResolveScopeAsync(
            RunMaterialRequirementPlanCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ItemIds.Count > 0)
            {
                var named = await items.GetByIdsAsync(request.ItemIds, cancellationToken);

                var missing = request.ItemIds.Except(named.Select(i => i.Id)).ToArray();
                if (missing.Length > 0)
                    return Result.Failure<List<Item>, Error>(Errors.General.NotFound(missing[0].ToString(), nameof(Item)));

                return Result.Success<List<Item>, Error>([.. named]);
            }

            // Everything that somebody has set a reorder policy on. An item with no policy is one
            // nobody has decided how to replenish, and guessing on their behalf is not planning.
            var needingReorder = await inventories.GetNeedingReorderAsync(request.WarehouseId, cancellationToken);

            return Result.Success<List<Item>, Error>(
                [.. needingReorder.Select(i => i.Item).DistinctBy(i => i.Id)]);
        }

        /// <summary>
        /// One item, one line. Gross demand comes from the forecast; supply already on its way
        /// comes from open purchase and production orders, which is why both are consulted.
        /// </summary>
        private async Task<Result<MaterialRequirementPlanLine, Error>> PlanItemAsync(
            MaterialRequirementPlan plan,
            Item item,
            DemandForecast? forecast,
            RunMaterialRequirementPlanCommand request,
            CancellationToken cancellationToken)
        {
            // Every figure on the line is a quantity, and a quantity without a unit is a number
            // nobody can act on. Refused by name rather than left to fail as an argument error
            // halfway through, which would take the whole run down with it.
            if (item.BaseUnitOfMeasureId is null || item.BaseUnitOfMeasureId == Guid.Empty)
                return Result.Failure<MaterialRequirementPlanLine, Error>(
                    Errors.Planning.ItemHasNoUnitOfMeasure(item.Name ?? item.Id.ToString()));

            var grossRequirement = forecast?.TotalFor(item.Id) ?? 0m;

            var onOrder = await purchaseOrders.GetQuantityOnOrderAsync(item.Id, request.WarehouseId, cancellationToken);
            var inProduction = await productionOrders.GetQuantityInProductionAsync(item.Id, cancellationToken);

            var onHand = request.WarehouseId is not null
                ? (await inventories.GetAsync(item.Id, request.WarehouseId.Value, cancellationToken: cancellationToken))?.OnHandQuantity ?? 0m
                : await inventories.GetTotalAvailableAsync(item.Id, cancellationToken);

            var line = new MaterialRequirementPlanLine(
                plan,
                item,
                request.HorizonEnd,
                grossRequirement,
                onOrder + inProduction,
                onHand,
                item.BaseUnitOfMeasureId.Value);

            var policy = request.WarehouseId is not null
                ? await inventories.GetAsync(item.Id, request.WarehouseId.Value, cancellationToken: cancellationToken)
                : null;

            if (policy is not null)
                line.SetSafetyQuantity(policy.MinimumQuantity);

            if (line.NetRequirement > 0m)
            {
                // Made items become production orders and bought items become requisitions. An
                // item that is neither has a shortage nobody can act on, so it is left as a line
                // that records the gap without proposing a way to close it.
                var action = item.IsManufactured
                    ? PlannedOrderAction.Manufacture
                    : item.IsPurchasable
                        ? PlannedOrderAction.Purchase
                        : PlannedOrderAction.None;

                if (action != PlannedOrderAction.None)
                {
                    var quantity = policy?.ReorderQuantity is > 0m
                        ? Math.Max(line.NetRequirement, policy.ReorderQuantity.Value)
                        : line.NetRequirement;

                    line.Suggest(action, quantity, leadTimeDays: 0);
                }
            }

            return Result.Success<MaterialRequirementPlanLine, Error>(line);
        }
    }
}
