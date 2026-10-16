using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Domain.Repositories.Modules.WarehouseManagement;
/// <summary>
/// Transfers between warehouses - حواله انتقال. The in-transit query is the one that matters:
/// that stock belongs to neither end and nobody finds it by walking around.
/// </summary>
public interface IStockTransferOrderRepository : IRepository<StockTransferOrder>
{
    Task<StockTransferOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    /// <summary>
    /// A transfer with its lines and the items on them, which is the only form in which it can be
    /// dispatched or received: both need to move real stock.
    /// </summary>
    Task<StockTransferOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// What has left one warehouse and not yet arrived at another. This is stock the business owns
    /// and neither warehouse can see, so it is the one balance nobody reconciles by walking around.
    /// </summary>
    Task<IReadOnlyList<StockTransferOrder>> GetInTransitAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockTransferOrder>> GetAwaitingApprovalAsync(Guid? sourceWarehouseId = null, CancellationToken cancellationToken = default);

    /// <summary>Transfers that arrived short, which somebody has to write off or chase.</summary>
    Task<IReadOnlyList<StockTransferOrder>> GetWithDiscrepancyAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
