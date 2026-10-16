using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;
/// <summary>
/// Batches - سری ساخت. What a recall traces and what an expiry sweep reads.
/// </summary>
public interface IItemBatchRepository : IRepository<ItemBatch>
{
    Task<ItemBatch?> GetByNumberAsync(Guid itemId, string batchNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batches available to issue, oldest expiry first. Issuing in this order is what stops stock
    /// expiring on the shelf while newer stock is shipped.
    /// </summary>
    Task<IReadOnlyList<ItemBatch>> GetAvailableForIssueAsync(Guid itemId, Guid? warehouseId = null, CancellationToken cancellationToken = default);

    /// <summary>Batches that expire within the window, so they can be sold through or written off.</summary>
    Task<IReadOnlyList<ItemBatch>> GetExpiringBeforeAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Batches already past their date that still hold stock.</summary>
    Task<IReadOnlyList<ItemBatch>> GetExpiredWithStockAsync(DateOnly asOf, CancellationToken cancellationToken = default);
}
