using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class ItemBatchRepository(IMainDbContext context) : Repository<ItemBatch>(context), IItemBatchRepository
{
    public async Task<ItemBatch?> GetByNumberAsync(Guid itemId, string batchNumber, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(
            b => b.ItemId == itemId && b.BatchNumber == batchNumber,
            cancellationToken);
    }

    /// <summary>
    /// Ordered by expiry, then by production date for batches that never expire. Issuing in this
    /// order is what stops older stock sitting behind newer stock until it is worthless.
    /// </summary>
    public async Task<IReadOnlyList<ItemBatch>> GetAvailableForIssueAsync(Guid itemId, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Where(b => b.ItemId == itemId
                        && b.Status == ItemBatchStatus.Available
                        && b.RemainingQuantity > 0
                        && (warehouseId == null || b.WarehouseId == warehouseId))
            .OrderBy(b => b.ExpirationDate ?? DateOnly.MaxValue)
            .ThenBy(b => b.ProductionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemBatch>> GetExpiringBeforeAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(b => b.Item)
            .Where(b => b.ExpirationDate != null
                        && b.ExpirationDate <= date
                        && b.RemainingQuantity > 0
                        && b.Status == ItemBatchStatus.Available)
            .OrderBy(b => b.ExpirationDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemBatch>> GetExpiredWithStockAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(b => b.Item)
            .Where(b => b.ExpirationDate != null
                        && b.ExpirationDate < asOf
                        && b.RemainingQuantity > 0)
            .OrderBy(b => b.ExpirationDate)
            .ToListAsync(cancellationToken);
    }
}
