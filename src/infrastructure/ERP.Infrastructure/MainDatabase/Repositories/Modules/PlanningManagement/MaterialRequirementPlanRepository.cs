using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.PlanningManagement;
using ERP.Domain.Repositories.Modules.PlanningManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.PlanningManagement;

public class MaterialRequirementPlanRepository(IMainDbContext context) : Repository<MaterialRequirementPlan>(context), IMaterialRequirementPlanRepository
{
    public async Task<MaterialRequirementPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<MaterialRequirementPlan?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<MaterialRequirementPlan?> GetLatestCompletedAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.Status == MaterialRequirementPlanStatus.Completed
                        && (warehouseId == null || p.WarehouseId == warehouseId))
            .OrderByDescending(p => p.CompletedOn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Tracked, because the caller releases these into requisitions and marks each one as it goes.
    /// </summary>
    public async Task<IReadOnlyList<MaterialRequirementPlanLine>> GetPendingReleaseLinesAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.MaterialRequirementPlanLines
            .Include(l => l.Item)
            .Where(l => l.PlanId == planId
                        && l.SuggestedAction != PlannedOrderAction.None
                        && l.ReleasedOn == null)
            .OrderBy(l => l.SuggestedReleaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialRequirementPlanLine>> GetPastDueLinesAsync(Guid planId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await _context.MaterialRequirementPlanLines
            .AsNoTracking()
            .Include(l => l.Item)
            .Where(l => l.PlanId == planId
                        && l.SuggestedAction != PlannedOrderAction.None
                        && l.ReleasedOn == null
                        && l.SuggestedReleaseDate < asOf)
            .OrderBy(l => l.SuggestedReleaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialRequirementPlanLine>> GetLinesForItemAsync(Guid planId, Guid itemId, CancellationToken cancellationToken = default)
    {
        return await _context.MaterialRequirementPlanLines
            .AsNoTracking()
            .Where(l => l.PlanId == planId && l.ItemId == itemId)
            .OrderBy(l => l.RequiredOn)
            .ToListAsync(cancellationToken);
    }
}
