using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;

/// <summary>
/// The inventory card. Entries are appended and never edited, so this repository reads far more
/// than it writes.
/// </summary>
public interface IStockLedgerEntryRepository : IRepository<StockLedgerEntry>
{
    /// <summary>
    /// The last entry for an item in a warehouse, which carries the running balance a new movement
    /// must continue from.
    /// </summary>
    Task<StockLedgerEntry?> GetLastEntryAsync(Guid itemId, Guid warehouseId, CancellationToken cancellationToken = default);

    /// <summary>Every movement of an item in a warehouse over a period, in the order it happened.</summary>
    Task<IReadOnlyList<StockLedgerEntry>> GetCardAsync(
        Guid itemId,
        Guid warehouseId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The quantity and value on hand at a moment in the past, which is what an inventory valuation
    /// at a period end needs.
    /// </summary>
    Task<(decimal Quantity, decimal Value)> GetBalanceAtAsync(
        Guid itemId,
        Guid warehouseId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default);

    /// <summary>The entries a business document produced, for tracing a movement back to its cause.</summary>
    Task<IReadOnlyList<StockLedgerEntry>> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    /// <summary>Everything that touched one batch, which is what a recall traces.</summary>
    Task<IReadOnlyList<StockLedgerEntry>> GetByBatchAsync(Guid itemBatchId, CancellationToken cancellationToken = default);
}
