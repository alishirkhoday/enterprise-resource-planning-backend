using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;
using ERP.Application.UseCases.Modules.ProductionManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.CompleteProductionOrder
{
    /// <summary>
    /// Closes a works order and receives what it made into stock - تکمیل سفارش تولید.
    /// <para>
    /// The finished goods are valued at what the run actually cost - the material it consumed plus
    /// the labour and overhead booked to it - divided over the good output alone. Scrap is costed
    /// but never becomes stock, which is what makes a bad run show up as an expensive product
    /// rather than disappearing into a quantity nobody has.
    /// </para>
    /// </summary>
    public class CompleteProductionOrderCommandHandler(
        IProductionOrderRepository productionOrders,
        IItemInventoryRepository inventories,
        IStockLedgerEntryRepository stockLedger,
        TimeProvider timeProvider)
        : IRequestHandler<CompleteProductionOrderCommand, Result<ProductionOrderDto, Error>>
    {
        public async Task<Result<ProductionOrderDto, Error>> Handle(CompleteProductionOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await productionOrders.GetWithTasksAsync(request.ProductionOrderId, cancellationToken);
            if (order is null)
                return Result.Failure<ProductionOrderDto, Error>(Errors.General.NotFound(request.ProductionOrderId.ToString(), nameof(ProductionOrder)));

            if (request.ProducedQuantity <= 0m && request.ScrappedQuantity <= 0m)
                return Result.Failure<ProductionOrderDto, Error>(Errors.Production.NothingWasProduced());

            var warehouseId = request.WarehouseId ?? order.WarehouseId;
            if (warehouseId is null && request.ProducedQuantity > 0m)
                return Result.Failure<ProductionOrderDto, Error>(
                    Errors.Production.OrderCannotBeChanged("The order names no warehouse, so there is nowhere to receive the output."));

            if (order.Item.BaseUnitOfMeasureId is null && order.UnitOfMeasureId is null)
                return Result.Failure<ProductionOrderDto, Error>(
                    Errors.Warehouse.ItemHasNoUnitOfMeasure(order.Item.Name ?? order.ItemId.ToString()));

            try
            {
                order.RecordOutput(request.ProducedQuantity, request.ScrappedQuantity);

                // Booked before the unit cost is worked out, because they are part of what the
                // run cost and leaving them out would undervalue everything it made.
                if (request.LabourCost > 0m || request.OverheadCost > 0m)
                {
                    order.AccumulateCost(
                        Money.Zero(order.CurrencyCode),
                        Money.Create(request.LabourCost, order.CurrencyCode),
                        Money.Create(request.OverheadCost, order.CurrencyCode));
                }
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<ProductionOrderDto, Error>(Errors.Production.OrderCannotBeChanged(exception.Message));
            }

            if (request.ProducedQuantity > 0m)
            {
                var received = await ReceiveOutputAsync(order, warehouseId!.Value, request, cancellationToken);
                if (received.IsFailure)
                    return Result.Failure<ProductionOrderDto, Error>(received.Error);
            }

            try
            {
                order.Complete();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<ProductionOrderDto, Error>(Errors.Production.OrderCannotBeChanged(exception.Message));
            }

            await productionOrders.UpdateAsync(order, cancellationToken);

            return Result.Success<ProductionOrderDto, Error>(order.ToDto());
        }

        private async Task<Result<bool, Error>> ReceiveOutputAsync(
            ProductionOrder order,
            Guid warehouseId,
            CompleteProductionOrderCommand request,
            CancellationToken cancellationToken)
        {
            var inventory = await inventories.GetForUpdateAsync(order.ItemId, warehouseId, request.StorageLocationId, cancellationToken);
            if (inventory is null)
                return Result.Failure<bool, Error>(
                    Errors.Warehouse.NoInventoryRecord(order.Item.Name ?? order.ItemId.ToString(), warehouseId.ToString()));

            if (inventory.TotalValue.CurrencyCode != order.CurrencyCode)
                return Result.Failure<bool, Error>(Errors.Production.OrderCannotBeChanged(
                    $"The warehouse carries stock in {inventory.TotalValue.CurrencyCode} but the order is costed in {order.CurrencyCode}."));

            // Everything the run cost, over the good output only. The domain works this out the
            // same way, which is why the whole cost is booked before this is read.
            var value = order.UnitCost.Multiply(request.ProducedQuantity);

            inventory.Increase(request.ProducedQuantity, value);

            var entry = new StockLedgerEntry(
                order.Item,
                warehouseId,
                StockMovementType.ProductionReceipt,
                request.ProducedQuantity,
                value,
                order.UnitOfMeasureId ?? order.Item.BaseUnitOfMeasureId!.Value,
                inventory.OnHandQuantity,
                inventory.TotalValue,
                timeProvider.GetUtcNow());

            entry.SetTracking(request.ItemBatchId, null);
            entry.SetReference(nameof(ProductionOrder), order.Id.ToString(), order.Code);
            entry.SetDescription(request.Description);

            if (request.StorageLocationId is not null)
                entry.ScopeToStorageLocation(request.StorageLocationId.Value);

            await stockLedger.CreateAsync(entry, cancellationToken);

            return Result.Success<bool, Error>(true);
        }
    }
}
