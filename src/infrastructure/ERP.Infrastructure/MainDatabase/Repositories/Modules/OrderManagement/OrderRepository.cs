using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.OrderManagement;
using ERP.Domain.Repositories.Modules.OrderManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.OrderManagement;

public class OrderRepository(IMainDbContext context) : Repository<Order>(context), IOrderRepository
{
    public async Task<Order?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(o => o.Code == code, cancellationToken);
    }

    public async Task<Order?> GetWithItemsAndPaymentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Item)
            .Include(o => o.Payments)
            .Include(o => o.ShipmentStatusHistories)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.RegistrationDateTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Tracked, because the caller is about to cancel these and release the stock they hold. The
    /// default time value is excluded, which is what an order with no payment window looks like.
    /// </summary>
    public async Task<IReadOnlyList<Order>> GetExpiredUnpaidAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Pending
                        && o.TimeLeftToPayment != default
                        && o.TimeLeftToPayment < asOf)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetLastCodeAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .OrderByDescending(o => o.Code)
            .Select(o => o.Code)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
