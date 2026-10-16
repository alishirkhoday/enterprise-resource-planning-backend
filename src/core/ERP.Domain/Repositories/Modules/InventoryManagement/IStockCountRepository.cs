using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;
/// <summary>
/// Physical count sheets - انبارگردانی.
/// </summary>
public interface IStockCountRepository : IRepository<StockCount>
{
    Task<StockCount?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    /// <summary>A count with its sheet, which is the only form in which it can be worked on.</summary>
    Task<StockCount?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts that are planned or under way in a warehouse. A second count of the same shelves
    /// while one is open produces two sets of adjustments for the same stock.
    /// </summary>
    Task<IReadOnlyList<StockCount>> GetOpenAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockCount>> GetAwaitingApprovalAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// When an item was last counted anywhere, which is what a cycle counting plan schedules from.
    /// </summary>
    Task<DateOnly?> GetLastCountDateAsync(Guid itemId, Guid warehouseId, CancellationToken cancellationToken = default);
}
