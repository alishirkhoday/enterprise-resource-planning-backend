using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class StockLedgerEntryRepository(IMainDbContext context) : Repository<StockLedgerEntry>(context), IStockLedgerEntryRepository
{
    /// <summary>
    /// Ordered by the moment of the movement and then by when the row was written, so two
    /// movements recorded for the same instant still come back in the order they were entered.
    /// </summary>
    public async Task<StockLedgerEntry?> GetLastEntryAsync(Guid itemId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(e => e.ItemId == itemId && e.WarehouseId == warehouseId)
            .OrderByDescending(e => e.MovementDateTime)
            .ThenByDescending(e => e.CreationDateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockLedgerEntry>> GetCardAsync(
        Guid itemId,
        Guid warehouseId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(e => e.ItemId == itemId
                        && e.WarehouseId == warehouseId
                        && e.MovementDateTime >= from
                        && e.MovementDateTime <= to)
            .OrderBy(e => e.MovementDateTime)
            .ThenBy(e => e.CreationDateTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Read from the last entry at or before the moment rather than by summing every movement.
    /// Each entry already carries the running balance, which is the point of keeping a card.
    /// </summary>
    public async Task<(decimal Quantity, decimal Value)> GetBalanceAtAsync(
        Guid itemId,
        Guid warehouseId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        var entry = await Query
            .Where(e => e.ItemId == itemId
                        && e.WarehouseId == warehouseId
                        && e.MovementDateTime <= asOf)
            .OrderByDescending(e => e.MovementDateTime)
            .ThenByDescending(e => e.CreationDateTime)
            .Select(e => new { e.BalanceQuantity, Value = e.BalanceValue.Amount })
            .FirstOrDefaultAsync(cancellationToken);

        return (entry?.BalanceQuantity ?? 0m, entry?.Value ?? 0m);
    }

    public async Task<IReadOnlyList<StockLedgerEntry>> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(e => e.ReferenceType == referenceType && e.ReferenceId == referenceId)
            .OrderBy(e => e.MovementDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockLedgerEntry>> GetByBatchAsync(Guid itemBatchId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(e => e.ItemBatchId == itemBatchId)
            .OrderBy(e => e.MovementDateTime)
            .ToListAsync(cancellationToken);
    }
}
