using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.PlanningManagement;
using ERP.Domain.Repositories.Modules.PlanningManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.PlanningManagement;

public class CapacityPlanRepository(IMainDbContext context) : Repository<CapacityPlan>(context), ICapacityPlanRepository
{
    public async Task<CapacityPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<CapacityPlan?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(p => p.Lines).ThenInclude(l => l.WorkCenter)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<CapacityPlan?> GetLatestCompletedAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.Status == CapacityPlanStatus.Completed)
            .OrderByDescending(p => p.CompletedOn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// The overload comparison is written out in full rather than reusing the entity's computed
    /// properties, because <c>EffectiveHours</c> and <c>TotalLoadHours</c> have no columns behind
    /// them and would force the whole plan into memory to be evaluated.
    /// </summary>
    public async Task<IReadOnlyList<CapacityPlanLine>> GetOverloadedLinesAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.CapacityPlanLines
            .AsNoTracking()
            .Include(l => l.WorkCenter)
            .Where(l => l.PlanId == planId
                        && l.FirmLoadHours + l.PlannedLoadHours > l.AvailableHours * l.Efficiency.Value / 100m)
            .OrderByDescending(l => l.FirmLoadHours + l.PlannedLoadHours - l.AvailableHours * l.Efficiency.Value / 100m)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CapacityPlanLine>> GetLoadForWorkCenterAsync(Guid planId, Guid workCenterId, CancellationToken cancellationToken = default)
    {
        return await _context.CapacityPlanLines
            .AsNoTracking()
            .Where(l => l.PlanId == planId && l.WorkCenterId == workCenterId)
            .OrderBy(l => l.BucketStartDate)
            .ToListAsync(cancellationToken);
    }
}
