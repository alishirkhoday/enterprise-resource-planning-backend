using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class SaleOrderRepository(IMainDbContext context) : Repository<SaleOrder>(context), ISaleOrderRepository
{
    /// <summary>States in which something is still owed to the customer.</summary>
    private static readonly SaleOrderStatus[] OpenStatuses =
    [
        SaleOrderStatus.Confirmed,
        SaleOrderStatus.PartiallyDelivered
    ];

    public async Task<SaleOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(o => o.Number == number, cancellationToken);
    }

    public async Task<SaleOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SaleOrder>> GetOpenAsync(Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Customer)
            .Where(o => OpenStatuses.Contains(o.Status) && (customerId == null || o.CustomerId == customerId))
            .OrderBy(o => o.RequestedDeliveryDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// What is promised and not yet shipped. Subtracting this from what is on hand is what stops
    /// the same unit being sold to two customers.
    /// </summary>
    public async Task<decimal> GetQuantityCommittedAsync(Guid itemId, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await _context.SaleOrderItems
            .AsNoTracking()
            .Where(i => i.ItemId == itemId
                        && OpenStatuses.Contains(i.SaleOrder.Status)
                        && (warehouseId == null || i.WarehouseId == warehouseId || i.SaleOrder.WarehouseId == warehouseId))
            .SumAsync(i => i.Quantity - i.DeliveredQuantity, cancellationToken);
    }

    public async Task<IReadOnlyList<SaleOrder>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Customer)
            .Where(o => OpenStatuses.Contains(o.Status)
                        && o.RequestedDeliveryDate != null
                        && o.RequestedDeliveryDate < asOf)
            .OrderBy(o => o.RequestedDeliveryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetLastNumberAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .OrderByDescending(o => o.Number)
            .Select(o => o.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
