using ERP.Domain.Entities.Modules.OrderManagement;

namespace ERP.Domain.Repositories.Modules.OrderManagement;
/// <summary>
/// Customer orders taken through the storefront, as against the sales orders the back office
/// raises. They share a shape but not a lifecycle.
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// An order with its lines and payments. Whether it is paid is a question about the whole
    /// aggregate, so the whole aggregate is loaded.
    /// </summary>
    Task<Order?> GetWithItemsAndPaymentsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Orders whose payment window has closed without being paid. A background job cancels these
    /// and releases the stock they were holding.
    /// </summary>
    Task<IReadOnlyList<Order>> GetExpiredUnpaidAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);

    Task<string?> GetLastCodeAsync(CancellationToken cancellationToken = default);
}
