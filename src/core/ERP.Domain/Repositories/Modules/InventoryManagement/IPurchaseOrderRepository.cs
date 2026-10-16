using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;
/// <summary>
/// Purchase orders - سفارش خرید.
/// </summary>
public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    /// <summary>An order with its lines, which is the only form in which it can be received against.</summary>
    Task<PurchaseOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Orders that still have something outstanding to arrive.</summary>
    Task<IReadOnlyList<PurchaseOrder>> GetOpenAsync(Guid? supplierId = null, CancellationToken cancellationToken = default);

    /// <summary>Orders whose delivery date has passed with goods still owed.</summary>
    Task<IReadOnlyList<PurchaseOrder>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// How much of an item is on order and not yet received. Replenishment counts this so it does
    /// not order the same shortage twice.
    /// </summary>
    Task<decimal> GetQuantityOnOrderAsync(Guid itemId, Guid? warehouseId = null, CancellationToken cancellationToken = default);

    Task<string?> GetLastNumberAsync(CancellationToken cancellationToken = default);
}
