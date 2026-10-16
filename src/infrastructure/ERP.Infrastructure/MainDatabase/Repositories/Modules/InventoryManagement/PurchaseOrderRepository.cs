using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class PurchaseOrderRepository(IMainDbContext context) : Repository<PurchaseOrder>(context), IPurchaseOrderRepository
{
    /// <summary>States in which something is still expected to arrive.</summary>
    private static readonly PurchaseOrderStatus[] OpenStatuses =
    [
        PurchaseOrderStatus.Approved,
        PurchaseOrderStatus.PartiallyReceived
    ];

    public async Task<PurchaseOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(o => o.Number == number, cancellationToken);
    }

    public async Task<PurchaseOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetOpenAsync(Guid? supplierId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Supplier)
            .Where(o => OpenStatuses.Contains(o.Status) && (supplierId == null || o.SupplierId == supplierId))
            .OrderBy(o => o.ExpectedDeliveryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Supplier)
            .Where(o => OpenStatuses.Contains(o.Status)
                        && o.ExpectedDeliveryDate != null
                        && o.ExpectedDeliveryDate < asOf)
            .OrderBy(o => o.ExpectedDeliveryDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Summed from the order lines rather than the headers, because an order is often partly
    /// received and only the outstanding part counts as incoming supply.
    /// </summary>
    public async Task<decimal> GetQuantityOnOrderAsync(Guid itemId, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrderItems
            .AsNoTracking()
            .Where(i => i.ItemId == itemId
                        && OpenStatuses.Contains(i.PurchaseOrder.Status)
                        && (warehouseId == null || i.PurchaseOrder.WarehouseId == warehouseId))
            .SumAsync(i => i.Quantity - i.ReceivedQuantity, cancellationToken);
    }

    public async Task<string?> GetLastNumberAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .OrderByDescending(o => o.Number)
            .Select(o => o.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
