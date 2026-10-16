using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;
using ERP.Application.UseCases.Modules.ProductionManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.IssueMaterial
{
    /// <summary>
    /// Takes material out of the warehouse and charges it to a works order - مصرف مواد.
    /// <para>
    /// Three things move together here: the stock comes off the shelf, the ledger records why, and
    /// the order's material cost goes up by what it took. Any one of them alone is a lie - stock
    /// that vanished with no reason, or an order that made something out of nothing.
    /// </para>
    /// <para>
    /// What each line costs is read from the warehouse, not from the bill of materials. The bill
    /// says how much of a thing a product needs; what that quantity was worth is whatever the
    /// warehouse was carrying it at on the day it was taken.
    /// </para>
    /// </summary>
    public class IssueMaterialCommandHandler(
        IProductionOrderRepository productionOrders,
        IBillOfMaterialsRepository billsOfMaterials,
        IItemRepository items,
        IItemInventoryRepository inventories,
        IStockLedgerEntryRepository stockLedger,
        IMaterialIssueRepository materialIssues,
        INumberingSeriesRepository numberingSeries)
        : IRequestHandler<IssueMaterialCommand, Result<MaterialIssueDto, Error>>
    {
        private const string MaterialIssueNumberingKey = "MaterialIssue";

        public async Task<Result<MaterialIssueDto, Error>> Handle(IssueMaterialCommand request, CancellationToken cancellationToken)
        {
            var order = await productionOrders.GetWithTasksAsync(request.ProductionOrderId, cancellationToken);
            if (order is null)
                return Result.Failure<MaterialIssueDto, Error>(Errors.General.NotFound(request.ProductionOrderId.ToString(), nameof(ProductionOrder)));

            if (order.BillOfMaterialsId is null)
                return Result.Failure<MaterialIssueDto, Error>(
                    Errors.Production.NoBillOfMaterials(order.Item.Name ?? order.ItemId.ToString(), request.IssueDate));

            var bill = await billsOfMaterials.GetWithComponentsAsync(order.BillOfMaterialsId.Value, cancellationToken);
            if (bill is null)
                return Result.Failure<MaterialIssueDto, Error>(
                    Errors.General.NotFound(order.BillOfMaterialsId.Value.ToString(), nameof(BillOfMaterials)));

            // What the recipe calls for at this order's size, keyed by component. Everything below
            // is measured against it: a line not on the bill, or more of one than the run needs.
            var required = bill.Explode(order.Quantity).ToDictionary(c => c.ComponentId, c => c);

            var wanted = await ResolveRequestedLinesAsync(request, order, required, cancellationToken);
            if (wanted.IsFailure)
                return Result.Failure<MaterialIssueDto, Error>(wanted.Error);

            if (wanted.Value.Count == 0)
                return Result.Failure<MaterialIssueDto, Error>(
                    Errors.Production.MaterialCannotBeIssued("Every component this order needs has already been issued."));

            var series = await numberingSeries.GetForUpdateAsync(MaterialIssueNumberingKey, order.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<MaterialIssueDto, Error>(Errors.Order.NumberingSeriesMissing(MaterialIssueNumberingKey));

            MaterialIssue issue;

            try
            {
                issue = new MaterialIssue(series.Next(request.IssueDate), order, request.WarehouseId, request.IssueDate, order.CurrencyCode);
            }
            catch (InvalidOperationException exception)
            {
                // An order that is not released or in progress. The domain guards this in the
                // constructor, so there is nothing to undo.
                return Result.Failure<MaterialIssueDto, Error>(Errors.Production.MaterialCannotBeIssued(exception.Message));
            }

            if (request.OperationSequenceNumber is not null)
                issue.SetOperation(request.OperationSequenceNumber.Value);

            if (request.IssuedByHumanId is not null)
                issue.SetIssuer(request.IssuedByHumanId.Value);

            issue.SetDescription(request.Description);

            var posted = await TakeFromStockAsync(issue, order, request, wanted.Value, required, cancellationToken);
            if (posted.IsFailure)
                return Result.Failure<MaterialIssueDto, Error>(posted.Error);

            // Confirming is what charges the order. Doing it last means an issue that could not be
            // fulfilled never touched the order's cost.
            issue.Confirm();

            await materialIssues.CreateAsync(issue, cancellationToken);
            await productionOrders.UpdateAsync(order, cancellationToken);

            return Result.Success<MaterialIssueDto, Error>(issue.ToDto());
        }

        /// <summary>
        /// What to take. Naming lines issues those; naming none issues what the bill still calls
        /// for, less what has already gone out, which is what a first issue against a works order
        /// nearly always means.
        /// </summary>
        private async Task<Result<List<MaterialIssueLineDto>, Error>> ResolveRequestedLinesAsync(
            IssueMaterialCommand request,
            ProductionOrder order,
            Dictionary<Guid, (Guid ComponentId, decimal Quantity, Guid UnitOfMeasureId)> required,
            CancellationToken cancellationToken)
        {
            if (request.Lines.Count > 0)
                return Result.Success<List<MaterialIssueLineDto>, Error>([.. request.Lines]);

            var outstanding = new List<MaterialIssueLineDto>(required.Count);

            foreach (var component in required.Values)
            {
                var alreadyIssued = await materialIssues.GetIssuedQuantityAsync(order.Id, component.ComponentId, cancellationToken);
                var remaining = component.Quantity - alreadyIssued;

                if (remaining <= 0m)
                    continue;

                outstanding.Add(new MaterialIssueLineDto
                {
                    ComponentId = component.ComponentId,
                    Quantity = remaining,
                    StorageLocationId = request.StorageLocationId
                });
            }

            return Result.Success<List<MaterialIssueLineDto>, Error>(outstanding);
        }

        private async Task<Result<bool, Error>> TakeFromStockAsync(
            MaterialIssue issue,
            ProductionOrder order,
            IssueMaterialCommand request,
            List<MaterialIssueLineDto> lines,
            Dictionary<Guid, (Guid ComponentId, decimal Quantity, Guid UnitOfMeasureId)> required,
            CancellationToken cancellationToken)
        {
            var components = await items.GetByIdsAsync([.. lines.Select(l => l.ComponentId)], cancellationToken);
            var byId = components.ToDictionary(i => i.Id);
            var movedAt = new DateTimeOffset(request.IssueDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

            short lineNumber = 1;

            foreach (var line in lines)
            {
                if (!byId.TryGetValue(line.ComponentId, out var component))
                    return Result.Failure<bool, Error>(Errors.General.NotFound(line.ComponentId.ToString(), nameof(Item)));

                var name = component.Name ?? component.Id.ToString();

                // Issuing something the product is not made of is either the wrong order or the
                // wrong component, and both put a cost on a run that did not incur it.
                if (!required.TryGetValue(line.ComponentId, out var plan))
                    return Result.Failure<bool, Error>(Errors.Production.ComponentIsNotOnTheBill(name));

                var alreadyIssued = await materialIssues.GetIssuedQuantityAsync(order.Id, line.ComponentId, cancellationToken);
                var wouldBe = alreadyIssued + line.Quantity;

                // A second issue for the same shortage is the commonest way an order quietly eats
                // twice the material it needed, and nothing downstream would ever show it.
                if (wouldBe > plan.Quantity)
                    return Result.Failure<bool, Error>(Errors.Production.MoreIssuedThanRequired(name, wouldBe, plan.Quantity));

                var storageLocationId = line.StorageLocationId ?? request.StorageLocationId;

                var inventory = await inventories.GetForUpdateAsync(component.Id, request.WarehouseId, storageLocationId, cancellationToken);
                if (inventory is null)
                    return Result.Failure<bool, Error>(Errors.Warehouse.NoInventoryRecord(name, request.WarehouseId.ToString()));

                if (line.Quantity > inventory.OnHandQuantity)
                    return Result.Failure<bool, Error>(Errors.Warehouse.InsufficientStock(name, line.Quantity, inventory.OnHandQuantity));

                if (inventory.TotalValue.CurrencyCode != order.CurrencyCode)
                    return Result.Failure<bool, Error>(Errors.Production.MaterialCannotBeIssued(
                        $"{name} is carried in {inventory.TotalValue.CurrencyCode} but the order is costed in {order.CurrencyCode}."));

                var value = inventory.CostOfIssuing(line.Quantity);
                inventory.Decrease(line.Quantity, value);

                var issueLine = new MaterialIssueItem(
                    issue,
                    component,
                    lineNumber++,
                    plan.Quantity,
                    line.Quantity,
                    plan.UnitOfMeasureId,
                    value.Divide(line.Quantity));

                issueLine.SetTracking(line.ItemBatchId, line.SerialNumberId);

                if (storageLocationId is not null)
                    issueLine.ScopeToStorageLocation(storageLocationId.Value);

                issueLine.SetDescription(line.Description);
                issue.AddItem(issueLine);

                var entry = new StockLedgerEntry(
                    component,
                    request.WarehouseId,
                    StockMovementType.ProductionIssue,
                    -line.Quantity,
                    value.Negate(),
                    plan.UnitOfMeasureId,
                    inventory.OnHandQuantity,
                    inventory.TotalValue,
                    movedAt);

                entry.SetTracking(line.ItemBatchId, line.SerialNumberId);
                entry.SetReference(nameof(ProductionOrder), order.Id.ToString(), order.Code);

                if (storageLocationId is not null)
                    entry.ScopeToStorageLocation(storageLocationId.Value);

                await stockLedger.CreateAsync(entry, cancellationToken);
            }

            return Result.Success<bool, Error>(true);
        }
    }
}
