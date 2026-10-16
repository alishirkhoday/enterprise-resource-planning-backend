using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;
using ERP.Application.UseCases.Modules.ProductionManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.CreateProductionOrder
{
    /// <summary>
    /// Raises a works order - صدور سفارش تولید - and plans it in the same step.
    /// <para>
    /// The recipe is pinned to the order as it is created, using the version approved and in
    /// effect on the planned start date. Bills of materials get revised, and an order that looked
    /// the recipe up later would be costed against a formula it was never built to.
    /// </para>
    /// </summary>
    public class CreateProductionOrderCommandHandler(
        IItemRepository items,
        IBillOfMaterialsRepository billsOfMaterials,
        IProductionOrderRepository productionOrders,
        INumberingSeriesRepository numberingSeries)
        : IRequestHandler<CreateProductionOrderCommand, Result<ProductionOrderDto, Error>>
    {
        private const string ProductionOrderNumberingKey = "ProductionOrder";

        public async Task<Result<ProductionOrderDto, Error>> Handle(CreateProductionOrderCommand request, CancellationToken cancellationToken)
        {
            var item = await items.GetByIdAsync(request.ItemId, cancellationToken);
            if (item is null)
                return Result.Failure<ProductionOrderDto, Error>(Errors.General.NotFound(request.ItemId.ToString(), nameof(Item)));

            // The domain refuses this too, with an exception. Catching it here turns a five
            // hundred into a sentence naming the item somebody needs to go and fix.
            if (!item.IsManufactured)
                return Result.Failure<ProductionOrderDto, Error>(
                    Errors.Production.ItemIsNotManufactured(item.Name ?? item.Id.ToString()));

            var bill = await ResolveBillAsync(request, cancellationToken);
            if (bill is null)
                return Result.Failure<ProductionOrderDto, Error>(
                    Errors.Production.NoBillOfMaterials(item.Name ?? item.Id.ToString(), request.PlannedStartDate));

            var series = await numberingSeries.GetForUpdateAsync(ProductionOrderNumberingKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<ProductionOrderDto, Error>(Errors.Order.NumberingSeriesMissing(ProductionOrderNumberingKey));

            var order = new ProductionOrder(
                series.Next(request.PlannedStartDate),
                item,
                request.Quantity,
                request.CurrencyCode.Trim().ToUpperInvariant());

            order.SetPriority(request.Priority);
            order.SetTargets(request.WarehouseId, request.CostCenterId, request.UnitOfMeasureId ?? item.BaseUnitOfMeasureId);

            if (request.BranchId is not null)
                order.AssignToBranch(request.BranchId.Value);

            if (request.ReferenceType is not null && request.ReferenceId is not null)
                order.SetSource(request.ReferenceType, request.ReferenceId);

            try
            {
                // The routing comes from the bill unless one was named, so the steps and the
                // recipe belong to the same version of how the thing is made.
                order.Plan(
                    bill.Id,
                    request.RoutingId ?? bill.RoutingId ?? Guid.Empty,
                    request.PlannedStartDate,
                    request.DueDate);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
            {
                return Result.Failure<ProductionOrderDto, Error>(Errors.Production.OrderCannotBeChanged(exception.Message));
            }

            await productionOrders.CreateAsync(order, cancellationToken);

            return Result.Success<ProductionOrderDto, Error>(order.ToDto());
        }

        /// <summary>
        /// A named bill is used as given; otherwise the approved default in effect on the start
        /// date. Naming one is how a trial or a rework version is run deliberately.
        /// </summary>
        private async Task<BillOfMaterials?> ResolveBillAsync(CreateProductionOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.BillOfMaterialsId is not null)
                return await billsOfMaterials.GetWithComponentsAsync(request.BillOfMaterialsId.Value, cancellationToken);

            return await billsOfMaterials.GetEffectiveForItemAsync(request.ItemId, request.PlannedStartDate, cancellationToken);
        }
    }
}
