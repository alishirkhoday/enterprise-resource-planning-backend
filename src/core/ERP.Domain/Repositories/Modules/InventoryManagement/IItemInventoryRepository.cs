using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;
/// <summary>
/// What is on hand, per item per place. The row that actually moves when stock moves.
/// </summary>
public interface IItemInventoryRepository : IRepository<ItemInventory>
{
    /// <summary>
    /// The stock record for one item in one place. Returned tracked, because the caller is about
    /// to move stock and the change has to be saved.
    /// </summary>
    Task<ItemInventory?> GetForUpdateAsync(Guid itemId, Guid warehouseId, Guid? storageLocationId = null, CancellationToken cancellationToken = default);

    Task<ItemInventory?> GetAsync(Guid itemId, Guid warehouseId, Guid? storageLocationId = null, CancellationToken cancellationToken = default);

    /// <summary>What is on hand of one item across every warehouse.</summary>
    Task<IReadOnlyList<ItemInventory>> GetByItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>The total free to promise for an item, wherever it happens to be.</summary>
    Task<decimal> GetTotalAvailableAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Stock that has fallen under its minimum, which is what a shortage report reads.</summary>
    Task<IReadOnlyList<ItemInventory>> GetBelowMinimumAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stock at or under its reorder point once what is already ordered is counted, which is the
    /// list a replenishment run turns into purchase requisitions.
    /// </summary>
    Task<IReadOnlyList<ItemInventory>> GetNeedingReorderAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItemInventory>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
