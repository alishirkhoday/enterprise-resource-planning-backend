using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.ProductionManagement;

public class ProductionOrderRepository(IMainDbContext context) : Repository<ProductionOrder>(context), IProductionOrderRepository
{
    /// <summary>States in which output is still expected from the order.</summary>
    private static readonly ProductionOrderStatus[] InFlightStatuses =
    [
        ProductionOrderStatus.Planned,
        ProductionOrderStatus.Released,
        ProductionOrderStatus.InProgress,
        ProductionOrderStatus.OnHold
    ];

    public async Task<ProductionOrder?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(o => o.Code == code, cancellationToken);
    }

    public async Task<ProductionOrder?> GetWithTasksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(o => o.Item)
            .Include(o => o.Tasks).ThenInclude(t => t.WorkCenter)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionOrder>> GetByStatusAsync(ProductionOrderStatus status, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Item)
            .Where(o => o.Status == status)
            .OrderBy(o => o.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionOrder>> GetLateAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Item)
            .Where(o => InFlightStatuses.Contains(o.Status) && o.DueDate != null && o.DueDate < asOf)
            .OrderBy(o => o.DueDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// What is still to come off the line. Planning treats this as incoming supply alongside what
    /// is on order from suppliers, so only the unproduced part counts.
    /// </summary>
    public async Task<decimal> GetQuantityInProductionAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(o => o.ItemId == itemId && InFlightStatuses.Contains(o.Status))
            .SumAsync(o => o.Quantity - o.ProducedQuantity, cancellationToken);
    }

    /// <summary>
    /// Tracked, and with its order, because a caller holding a task is normally about to attach
    /// something to it that has to be saved alongside.
    /// </summary>
    public async Task<ProductionTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductionTasks
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionTask>> GetScheduledTasksAsync(Guid workCenterId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        return await _context.ProductionTasks
            .AsNoTracking()
            .Include(t => t.Order)
            .Where(t => t.WorkCenterId == workCenterId
                        && t.PlannedStartTime != null
                        && t.PlannedStartTime < to
                        && (t.PlannedEndTime == null || t.PlannedEndTime > from)
                        && t.Status != ProductionTaskStatus.Cancelled)
            .OrderBy(t => t.PlannedStartTime)
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
