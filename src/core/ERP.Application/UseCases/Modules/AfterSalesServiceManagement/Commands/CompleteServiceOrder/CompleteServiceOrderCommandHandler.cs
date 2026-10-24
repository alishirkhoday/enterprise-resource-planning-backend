using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Mapping;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.CompleteServiceOrder
{
    /// <summary>
    /// Closes a visit: what was done, what it took, and what came off the van.
    /// <para>
    /// Parts leave stock whoever is paying. A part fitted under warranty is still gone from the
    /// shelf; it simply never reaches the customer's bill, and inventory that only moves on
    /// chargeable work drifts further from reality with every warranty call.
    /// </para>
    /// </summary>
    public class CompleteServiceOrderCommandHandler(
        IServiceOrderRepository serviceOrders,
        IServiceContractRepository serviceContracts,
        IItemRepository items,
        IItemInventoryRepository inventories,
        IStockLedgerEntryRepository stockLedger)
        : IRequestHandler<CompleteServiceOrderCommand, Result<ServiceOrderDto, Error>>
    {
        public async Task<Result<ServiceOrderDto, Error>> Handle(CompleteServiceOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await serviceOrders.GetWithPartsAsync(request.ServiceOrderId, cancellationToken);
            if (order is null)
                return Result.Failure<ServiceOrderDto, Error>(Errors.General.NotFound(request.ServiceOrderId.ToString(), nameof(ServiceOrder)));

            if (!order.IsOpen)
                return Result.Failure<ServiceOrderDto, Error>(Errors.AfterSales.ServiceOrderIsClosed(order.Status.ToString()));

            if (request.Parts.Count > 0 && order.WarehouseId is null)
                return Result.Failure<ServiceOrderDto, Error>(Errors.AfterSales.NoWarehouseToIssuePartsFrom(order.Number));

            var added = await AddPartsAsync(order, request, cancellationToken);
            if (added.IsFailure)
                return Result.Failure<ServiceOrderDto, Error>(added.Error);

            try
            {
                order.RecordLabour(
                    request.LabourHours,
                    Money.Create(request.LabourCharge, order.CurrencyCode),
                    Money.Create(request.TravelCharge, order.CurrencyCode));

                order.Complete(request.CompletedOn, request.WorkPerformed);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<ServiceOrderDto, Error>(Errors.AfterSales.ServiceOrderCannotBeCompleted(exception.Message));
            }

            var issued = await IssuePartsAsync(order, cancellationToken);
            if (issued.IsFailure)
                return Result.Failure<ServiceOrderDto, Error>(issued.Error);

            // A visit under a contract draws down its allowance. Done on completion rather than on
            // raising, because a call that was never attended has consumed nothing.
            if (order.ServiceContractId is not null)
            {
                var contract = await serviceContracts.GetByIdAsync(order.ServiceContractId.Value, cancellationToken);
                if (contract is not null && contract.Status == ServiceContractStatus.Active)
                    contract.RecordVisit();
            }

            await serviceOrders.UpdateAsync(order, cancellationToken);

            return Result.Success<ServiceOrderDto, Error>(order.ToDto());
        }

        private async Task<Result<bool, Error>> AddPartsAsync(
            ServiceOrder order,
            CompleteServiceOrderCommand request,
            CancellationToken cancellationToken)
        {
            if (request.Parts.Count == 0)
                return Result.Success<bool, Error>(true);

            var found = await items.GetByIdsAsync([.. request.Parts.Select(p => p.ItemId)], cancellationToken);
            var byId = found.ToDictionary(i => i.Id);

            short lineNumber = (short)(order.Parts.Count + 1);

            foreach (var part in request.Parts)
            {
                if (!byId.TryGetValue(part.ItemId, out var item))
                    return Result.Failure<bool, Error>(Errors.General.NotFound(part.ItemId.ToString(), nameof(Item)));

                var line = new ServiceOrderPart(
                    order,
                    item,
                    lineNumber++,
                    part.Quantity,
                    Money.Create(part.UnitPrice, order.CurrencyCode),
                    part.UnitOfMeasureId);
                order.AddPart(line);

                order.SetServiceOrderPartTracking(line.Id, part.ItemBatchId, part.SerialNumberId);
            }

            return Result.Success<bool, Error>(true);
        }

        private async Task<Result<bool, Error>> IssuePartsAsync(ServiceOrder order, CancellationToken cancellationToken)
        {
            if (order.Parts.Count == 0)
                return Result.Success<bool, Error>(true);

            var warehouseId = order.WarehouseId!.Value;

            foreach (var part in order.Parts)
            {
                var inventory = await inventories.GetForUpdateAsync(part.ItemId, warehouseId, cancellationToken: cancellationToken);
                if (inventory is null)
                    return Result.Failure<bool, Error>(Errors.AfterSales.NoStockForPart(part.Item.Name ?? part.ItemId.ToString()));

                // Costing the issue is itself a check on whether there is enough, so it belongs
                // inside the guard. Outside it, asking for more than is on hand threw out of the
                // handler instead of coming back as a refusal the caller could read.
                Money cost;
                try
                {
                    cost = inventory.CostOfIssuing(part.Quantity);
                    inventory.Decrease(part.Quantity, cost);
                }
                catch (InvalidOperationException)
                {
                    return Result.Failure<bool, Error>(
                        Errors.AfterSales.NotEnoughStockForPart(part.Item.Name ?? part.ItemId.ToString(), part.Quantity, inventory.OnHandQuantity));
                }

                var entry = new StockLedgerEntry(
                    part.Item,
                    warehouseId,
                    StockMovementType.ServiceIssue,
                    -part.Quantity,
                    cost.Negate(),
                    part.UnitOfMeasureId,
                    inventory.OnHandQuantity,
                    inventory.TotalValue,
                    DateTimeOffset.UtcNow);

                entry.SetTracking(part.ItemBatchId, part.SerialNumberId);
                await stockLedger.CreateAsync(entry, cancellationToken);
            }

            return Result.Success<bool, Error>(true);
        }
    }
}
