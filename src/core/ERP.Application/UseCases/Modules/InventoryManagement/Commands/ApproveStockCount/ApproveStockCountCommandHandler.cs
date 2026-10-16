using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Application.UseCases.Modules.InventoryManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApproveStockCount
{
    /// <summary>
    /// Approves a count and makes the books agree with the shelves.
    /// <para>
    /// This is the only place in the system where stock changes without anything having physically
    /// moved, which is exactly why it is a separate, approved step: every adjustment written here
    /// is somebody saying the records were wrong. Each one leaves a ledger entry naming the count
    /// it came from, so a variance can always be traced back to the sheet that found it.
    /// </para>
    /// </summary>
    public class ApproveStockCountCommandHandler(
        IStockCountRepository stockCounts,
        IItemInventoryRepository inventories,
        IStockLedgerEntryRepository stockLedger,
        TimeProvider timeProvider)
        : IRequestHandler<ApproveStockCountCommand, Result<StockCountDto, Error>>
    {
        public async Task<Result<StockCountDto, Error>> Handle(ApproveStockCountCommand request, CancellationToken cancellationToken)
        {
            var count = await stockCounts.GetWithItemsAsync(request.StockCountId, cancellationToken);
            if (count is null)
                return Result.Failure<StockCountDto, Error>(Errors.General.NotFound(request.StockCountId.ToString(), nameof(StockCount)));

            try
            {
                count.Approve(request.ApprovedByHumanId, request.AdjustmentDocumentId);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<StockCountDto, Error>(Errors.Warehouse.CountCannotBeChanged(exception.Message));
            }

            var adjusted = await AdjustInventoryAsync(count, cancellationToken);
            if (adjusted.IsFailure)
                return Result.Failure<StockCountDto, Error>(adjusted.Error);

            await stockCounts.UpdateAsync(count, cancellationToken);

            return Result.Success<StockCountDto, Error>(count.ToDto());
        }

        private async Task<Result<bool, Error>> AdjustInventoryAsync(StockCount count, CancellationToken cancellationToken)
        {
            var movedAt = timeProvider.GetUtcNow();

            foreach (var line in count.Items.Where(i => i.HasDiscrepancy))
            {
                var inventory = await inventories.GetForUpdateAsync(
                    line.ItemId,
                    count.WarehouseId,
                    line.StorageLocationId,
                    cancellationToken);

                if (inventory is null)
                    return Result.Failure<bool, Error>(
                        Errors.Warehouse.NoInventoryRecord(line.Item.Name ?? line.ItemId.ToString(), count.WarehouseId.ToString()));

                // The counted quantity is the truth, so the record is set to it rather than nudged
                // by the variance. Between freezing the sheet and approving it the books may have
                // moved again, and nudging would carry that drift into the corrected figure.
                var valueBefore = inventory.TotalValue;
                inventory.AdjustToCountedQuantity(line.CountedQuantity!.Value, count.CountDate);

                // The movement recorded is the change the inventory actually underwent, not the
                // variance the sheet computed at its own frozen cost. The two are normally the
                // same number, and when they are not it is the inventory that has to balance.
                var valueChange = inventory.TotalValue.Subtract(valueBefore);

                var entry = new StockLedgerEntry(
                    line.Item,
                    count.WarehouseId,
                    StockMovementType.CountAdjustment,
                    line.Variance,
                    valueChange,
                    line.UnitOfMeasureId,
                    inventory.OnHandQuantity,
                    inventory.TotalValue,
                    movedAt);

                if (line.ItemBatchId is not null)
                    entry.SetTracking(line.ItemBatchId, null);

                entry.SetReference(nameof(StockCount), count.Id.ToString(), count.Number);
                entry.SetDescription(line.DiscrepancyReason);

                if (line.StorageLocationId is not null)
                    entry.ScopeToStorageLocation(line.StorageLocationId.Value);

                if (count.AdjustmentDocumentId is not null)
                    entry.LinkToDocument(count.AdjustmentDocumentId.Value);

                await stockLedger.CreateAsync(entry, cancellationToken);
            }

            return Result.Success<bool, Error>(true);
        }
    }
}
