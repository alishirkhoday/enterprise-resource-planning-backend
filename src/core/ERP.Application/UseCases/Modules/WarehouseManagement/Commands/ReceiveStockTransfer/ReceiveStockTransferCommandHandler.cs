using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;
using ERP.Application.UseCases.Modules.WarehouseManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ReceiveStockTransfer
{
    /// <summary>
    /// Records goods arriving at the destination, and closes the transfer.
    /// <para>
    /// What arrives is valued at what left, not at what the destination happens to hold. A
    /// transfer that changed the cost of the goods would let anybody move stock between two
    /// warehouses to make a margin appear.
    /// </para>
    /// <para>
    /// A shortage is recorded on the line and leaves the transfer closed with a discrepancy. The
    /// missing stock is deliberately not written off here: it left one building and never reached
    /// another, and whether that is theft, damage or a miscount is somebody's decision, not a
    /// side effect of receiving the rest of the pallet.
    /// </para>
    /// </summary>
    public class ReceiveStockTransferCommandHandler(
        IStockTransferOrderRepository transfers,
        IItemInventoryRepository inventories,
        IStockLedgerEntryRepository stockLedger,
        TimeProvider timeProvider)
        : IRequestHandler<ReceiveStockTransferCommand, Result<StockTransferDto, Error>>
    {
        public async Task<Result<StockTransferDto, Error>> Handle(ReceiveStockTransferCommand request, CancellationToken cancellationToken)
        {
            var transfer = await transfers.GetWithItemsAsync(request.TransferId, cancellationToken);
            if (transfer is null)
                return Result.Failure<StockTransferDto, Error>(Errors.General.NotFound(request.TransferId.ToString(), nameof(StockTransferOrder)));

            if (transfer.Status != StockTransferStatus.InTransit)
                return Result.Failure<StockTransferDto, Error>(
                    Errors.Warehouse.TransferCannotBeChanged($"Only a transfer in transit can be received; this one is {transfer.Status}."));

            if (request.ReceiveDate < transfer.DispatchedOn)
                return Result.Failure<StockTransferDto, Error>(
                    Errors.Warehouse.TransferCannotBeChanged($"Goods cannot arrive on {request.ReceiveDate:yyyy-MM-dd}, before they left on {transfer.DispatchedOn:yyyy-MM-dd}."));

            var quantities = TransferLineResolver.Resolve(transfer, request.Lines, line => line.DispatchedQuantity);
            if (quantities.IsFailure)
                return Result.Failure<StockTransferDto, Error>(quantities.Error);

            var shortagesExplained = EnsureShortagesAreExplained(quantities.Value);
            if (shortagesExplained.IsFailure)
                return Result.Failure<StockTransferDto, Error>(shortagesExplained.Error);

            var posted = await ReceiveIntoDestinationAsync(transfer, quantities.Value, request.ReceiveDate, cancellationToken);
            if (posted.IsFailure)
                return Result.Failure<StockTransferDto, Error>(posted.Error);

            try
            {
                transfer.Receive(request.ReceiveDate);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<StockTransferDto, Error>(Errors.Warehouse.TransferCannotBeChanged(exception.Message));
            }

            await transfers.UpdateAsync(transfer, cancellationToken);

            return Result.Success<StockTransferDto, Error>(transfer.ToDto());
        }

        /// <summary>
        /// A shortage without a reason is refused before anything is written. Stock that left a
        /// building and did not arrive is the one thing a warehouse must never be able to record
        /// silently, and the reason is what somebody investigates from later.
        /// </summary>
        private static Result<bool, Error> EnsureShortagesAreExplained(List<ResolvedTransferLine> lines)
        {
            foreach (var resolved in lines)
            {
                if (resolved.Quantity >= resolved.Line.DispatchedQuantity)
                    continue;

                if (string.IsNullOrWhiteSpace(resolved.DiscrepancyReason))
                    return Result.Failure<bool, Error>(Errors.Warehouse.TransferCannotBeChanged(
                        $"{resolved.Line.DispatchedQuantity - resolved.Quantity:N2} of {resolved.Line.Item.Name ?? resolved.Line.ItemId.ToString()} is missing and no reason was given."));
            }

            return Result.Success<bool, Error>(true);
        }

        private async Task<Result<bool, Error>> ReceiveIntoDestinationAsync(
            StockTransferOrder transfer,
            List<ResolvedTransferLine> lines,
            DateOnly receiveDate,
            CancellationToken cancellationToken)
        {
            var movedAt = TransferLineResolver.MovementMoment(timeProvider, receiveDate);

            foreach (var resolved in lines)
            {
                var line = resolved.Line;

                try
                {
                    transfer.ReceiveStockTransferOrderItem(line.Id, resolved.Quantity);

                    if (resolved.DiscrepancyReason is not null)
                        transfer.ExplainStockTransferOrderItemDiscrepancy(line.Id, resolved.DiscrepancyReason);
                }
                catch (InvalidOperationException exception)
                {
                    return Result.Failure<bool, Error>(Errors.Warehouse.TransferCannotBeChanged(exception.Message));
                }

                // A line that arrived empty still closes the transfer, but there is no movement to
                // write: nothing reached the destination.
                if (resolved.Quantity == 0m)
                    continue;

                var inventory = await inventories.GetForUpdateAsync(
                    line.ItemId,
                    transfer.DestinationWarehouseId,
                    line.DestinationStorageLocationId,
                    cancellationToken);

                if (inventory is null)
                    return Result.Failure<bool, Error>(
                        Errors.Warehouse.NoInventoryRecord(line.Item.Name ?? line.ItemId.ToString(), transfer.DestinationWarehouse.Name));

                var value = line.UnitCost.Multiply(resolved.Quantity);
                inventory.Increase(resolved.Quantity, value);

                var entry = new StockLedgerEntry(
                    line.Item,
                    transfer.DestinationWarehouseId,
                    StockMovementType.TransferIn,
                    resolved.Quantity,
                    value,
                    line.UnitOfMeasureId,
                    inventory.OnHandQuantity,
                    inventory.TotalValue,
                    movedAt);

                entry.SetTracking(line.ItemBatchId, null);
                entry.SetReference(nameof(StockTransferOrder), transfer.Id.ToString(), transfer.Number);

                if (line.DestinationStorageLocationId is not null)
                    entry.ScopeToStorageLocation(line.DestinationStorageLocationId.Value);

                await stockLedger.CreateAsync(entry, cancellationToken);
            }

            return Result.Success<bool, Error>(true);
        }
    }
}
