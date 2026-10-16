using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class StockCountRepository(IMainDbContext context) : Repository<StockCount>(context), IStockCountRepository
{
    private static readonly StockCountStatus[] OpenStatuses =
    [
        StockCountStatus.Planned,
        StockCountStatus.InProgress,
        StockCountStatus.AwaitingApproval
    ];

    public async Task<StockCount?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(c => c.Number == number, cancellationToken);
    }

    public async Task<StockCount?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StockCount>> GetOpenAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => OpenStatuses.Contains(c.Status) && (warehouseId == null || c.WarehouseId == warehouseId))
            .OrderBy(c => c.CountDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockCount>> GetAwaitingApprovalAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.Status == StockCountStatus.AwaitingApproval && (warehouseId == null || c.WarehouseId == warehouseId))
            .OrderBy(c => c.CountDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Only a completed count answers this. A count that was planned and abandoned tells a cycle
    /// plan nothing about whether the shelf was ever looked at.
    /// </summary>
    public async Task<DateOnly?> GetLastCountDateAsync(Guid itemId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var dates = await Query
            .Where(c => c.Status == StockCountStatus.Completed
                        && c.WarehouseId == warehouseId
                        && c.Items.Any(i => i.ItemId == itemId))
            .OrderByDescending(c => c.CountDate)
            .Select(c => (DateOnly?)c.CountDate)
            .Take(1)
            .ToListAsync(cancellationToken);

        return dates.FirstOrDefault();
    }
}
