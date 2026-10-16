using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.WarehouseManagement;

public class StockTransferOrderRepository(IMainDbContext context) : Repository<StockTransferOrder>(context), IStockTransferOrderRepository
{
    public async Task<StockTransferOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(t => t.Number == number, cancellationToken);
    }

    public async Task<StockTransferOrder?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .Include(t => t.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <summary>
    /// Either end of the move matches, because a transfer in transit is equally a warehouse's
    /// problem whether it is waiting for the goods or answerable for having sent them.
    /// </summary>
    public async Task<IReadOnlyList<StockTransferOrder>> GetInTransitAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(t => t.Status == StockTransferStatus.InTransit
                        && (warehouseId == null
                            || t.SourceWarehouseId == warehouseId
                            || t.DestinationWarehouseId == warehouseId))
            .OrderBy(t => t.ExpectedArrivalDate ?? t.RequestDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransferOrder>> GetAwaitingApprovalAsync(Guid? sourceWarehouseId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(t => t.Status == StockTransferStatus.Draft
                        && (sourceWarehouseId == null || t.SourceWarehouseId == sourceWarehouseId))
            .OrderBy(t => t.RequestDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransferOrder>> GetWithDiscrepancyAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(t => t.Items).ThenInclude(i => i.Item)
            .Where(t => t.Status == StockTransferStatus.ReceivedWithDiscrepancy
                        && t.ReceivedOn >= from
                        && t.ReceivedOn <= to)
            .OrderByDescending(t => t.ReceivedOn)
            .ToListAsync(cancellationToken);
    }
}
