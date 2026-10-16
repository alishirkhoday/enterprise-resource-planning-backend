using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;
using ERP.Application.UseCases.Modules.WarehouseManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.DispatchStockTransfer
{
    /// <summary>
    /// Records goods leaving the source warehouse.
    /// <para>
    /// This is the half of a transfer people forget is a real movement. The stock comes off the
    /// source's books here and does not appear on the destination's until it is received, so
    /// between the two it belongs to neither and is visible only on the transfer itself.
    /// </para>
    /// </summary>
    public class DispatchStockTransferCommandHandler(
        IStockTransferOrderRepository transfers,
        IItemInventoryRepository inventories,
        IStockLedgerEntryRepository stockLedger,
        TimeProvider timeProvider)
        : IRequestHandler<DispatchStockTransferCommand, Result<StockTransferDto, Error>>
    {
        public async Task<Result<StockTransferDto, Error>> Handle(DispatchStockTransferCommand request, CancellationToken cancellationToken)
        {
            var transfer = await transfers.GetWithItemsAsync(request.TransferId, cancellationToken);
            if (transfer is null)
                return Result.Failure<StockTransferDto, Error>(Errors.General.NotFound(request.TransferId.ToString(), nameof(StockTransferOrder)));

            if (transfer.Status != StockTransferStatus.Approved)
                return Result.Failure<StockTransferDto, Error>(
                    Errors.Warehouse.TransferCannotBeChanged($"Only an approved transfer can be dispatched; this one is {transfer.Status}."));

            var quantities = TransferLineResolver.Resolve(transfer, request.Lines, line => line.RequestedQuantity);
            if (quantities.IsFailure)
                return Result.Failure<StockTransferDto, Error>(quantities.Error);

            var posted = await IssueFromSourceAsync(transfer, quantities.Value, request.DispatchDate, cancellationToken);
            if (posted.IsFailure)
                return Result.Failure<StockTransferDto, Error>(posted.Error);

            if (request.VehicleNumber is not null)
                transfer.SetVehicleNumber(request.VehicleNumber);

            try
            {
                transfer.Dispatch(request.DispatchDate);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<StockTransferDto, Error>(Errors.Warehouse.TransferCannotBeChanged(exception.Message));
            }

            await transfers.UpdateAsync(transfer, cancellationToken);

            return Result.Success<StockTransferDto, Error>(transfer.ToDto());
        }

        private async Task<Result<bool, Error>> IssueFromSourceAsync(
            StockTransferOrder transfer,
            List<ResolvedTransferLine> lines,
            DateOnly dispatchDate,
            CancellationToken cancellationToken)
        {
            var movedAt = TransferLineResolver.MovementMoment(timeProvider, dispatchDate);

            foreach (var resolved in lines)
            {
                var line = resolved.Line;

                var inventory = await inventories.GetForUpdateAsync(
                    line.ItemId,
                    transfer.SourceWarehouseId,
                    line.SourceStorageLocationId,
                    cancellationToken);

                if (inventory is null)
                    return Result.Failure<bool, Error>(
                        Errors.Warehouse.NoInventoryRecord(line.Item.Name ?? line.ItemId.ToString(), transfer.SourceWarehouse.Name));

                // The source values its stock in one currency and the transfer was raised in
                // another. Converting here would invent a rate; refusing says which two disagree.
                if (inventory.TotalValue.CurrencyCode != transfer.CurrencyCode)
                    return Result.Failure<bool, Error>(Errors.Warehouse.TransferCannotBeChanged(
                        $"{transfer.SourceWarehouse.Name} carries {line.Item.Name ?? line.ItemId.ToString()} in {inventory.TotalValue.CurrencyCode} but the transfer is in {transfer.CurrencyCode}."));

                // Checked before the domain is asked to issue, because the domain answers with an
                // exception and the caller deserves a sentence naming the item and the shortfall.
                if (resolved.Quantity > inventory.OnHandQuantity)
                    return Result.Failure<bool, Error>(
                        Errors.Warehouse.InsufficientStock(line.Item.Name ?? line.ItemId.ToString(), resolved.Quantity, inventory.OnHandQuantity));

                // The value that leaves is what the source was carrying the stock at, not the cost
                // quoted when the transfer was raised, so the source's books balance to the penny.
                var value = inventory.CostOfIssuing(resolved.Quantity);

                try
                {
                    line.Dispatch(resolved.Quantity);

                    // Recorded on the line so the destination books in the same value, however
                    // long the pallet sat waiting and however the source's average moved meanwhile.
                    line.SetUnitCost(value.Divide(resolved.Quantity));
                }
                catch (InvalidOperationException exception)
                {
                    return Result.Failure<bool, Error>(Errors.Warehouse.TransferCannotBeChanged(exception.Message));
                }

                inventory.Decrease(resolved.Quantity, value);

                var entry = new StockLedgerEntry(
                    line.Item,
                    transfer.SourceWarehouseId,
                    StockMovementType.TransferOut,
                    -resolved.Quantity,
                    value.Negate(),
                    line.UnitOfMeasureId,
                    inventory.OnHandQuantity,
                    inventory.TotalValue,
                    movedAt);

                entry.SetTracking(line.ItemBatchId, null);
                entry.SetReference(nameof(StockTransferOrder), transfer.Id.ToString(), transfer.Number);

                if (line.SourceStorageLocationId is not null)
                    entry.ScopeToStorageLocation(line.SourceStorageLocationId.Value);

                await stockLedger.CreateAsync(entry, cancellationToken);
            }

            return Result.Success<bool, Error>(true);
        }
    }
}
