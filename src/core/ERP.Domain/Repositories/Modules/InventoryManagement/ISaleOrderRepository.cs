using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;
/// <summary>
/// Sales orders as the inventory side sees them: a commitment to ship something.
/// </summary>
public interface ISaleOrderRepository : IRepository<SaleOrder>
{
    Task<SaleOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    Task<SaleOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Orders with something still to deliver.</summary>
    Task<IReadOnlyList<SaleOrder>> GetOpenAsync(Guid? customerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// How much of an item is promised to customers and not yet shipped. Availability is what is
    /// on hand less this, so promising twice is what it prevents.
    /// </summary>
    Task<decimal> GetQuantityCommittedAsync(Guid itemId, Guid? warehouseId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleOrder>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    Task<string?> GetLastNumberAsync(CancellationToken cancellationToken = default);
}
