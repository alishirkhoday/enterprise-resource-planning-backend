using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class ItemInventoryRepository(IMainDbContext context) : Repository<ItemInventory>(context), IItemInventoryRepository
{
    /// <summary>
    /// Tracked, because the caller is about to move stock. The concurrency stamp comes with it,
    /// so two movements against the same balance cannot both commit on a stale read.
    /// </summary>
    public async Task<ItemInventory?> GetForUpdateAsync(Guid itemId, Guid warehouseId, Guid? storageLocationId = null, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(
            i => i.ItemId == itemId && i.WarehouseId == warehouseId && i.StorageLocationId == storageLocationId,
            cancellationToken);
    }

    public async Task<ItemInventory?> GetAsync(Guid itemId, Guid warehouseId, Guid? storageLocationId = null, CancellationToken cancellationToken = default)
    {
        return await Query.FirstOrDefaultAsync(
            i => i.ItemId == itemId && i.WarehouseId == warehouseId && i.StorageLocationId == storageLocationId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ItemInventory>> GetByItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(i => i.Warehouse)
            .Where(i => i.ItemId == itemId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Summed in the database. Available is what is on hand less what is already promised and
    /// what is held back, which is the only figure safe to promise against.
    /// </summary>
    public async Task<decimal> GetTotalAvailableAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(i => i.ItemId == itemId)
            .SumAsync(i => i.OnHandQuantity - i.ReservedQuantity - i.QuarantinedQuantity, cancellationToken);
    }

    public async Task<IReadOnlyList<ItemInventory>> GetBelowMinimumAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(i => i.Item)
            .Where(i => i.OnHandQuantity < i.MinimumQuantity
                        && (warehouseId == null || i.WarehouseId == warehouseId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// What is already on order counts towards the reorder point, so a shortage is not ordered
    /// twice while the first order is still in transit.
    /// </summary>
    public async Task<IReadOnlyList<ItemInventory>> GetNeedingReorderAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(i => i.Item)
            .Where(i => i.ReorderPoint != null
                        && i.OnHandQuantity + i.OnOrderQuantity <= i.ReorderPoint
                        && (warehouseId == null || i.WarehouseId == warehouseId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemInventory>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(i => i.Item)
            .Where(i => i.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
    }
}
