using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.SaleAndMarketingManagement;

public class SalesTargetRepository(IMainDbContext context) : Repository<SalesTarget>(context), ISalesTargetRepository
{
    /// <summary>
    /// Tracked rather than read only, because the caller is normally about to post an achievement
    /// against whatever comes back.
    /// </summary>
    public async Task<IReadOnlyList<SalesTarget>> GetForScopeAsync(SalesTargetScope scope, Guid scopeReferenceId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await TrackedQuery
            .Where(t => t.Scope == scope
                        && t.ScopeReferenceId == scopeReferenceId
                        && t.Period.StartDate <= onDate
                        && (t.Period.EndDate == null || t.Period.EndDate >= onDate))
            .OrderBy(t => t.Period.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SalesTarget>> GetActiveOnAsync(DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(t => t.Period.StartDate <= onDate && (t.Period.EndDate == null || t.Period.EndDate >= onDate))
            .OrderBy(t => t.Scope)
            .ThenBy(t => t.Title)
            .ToListAsync(cancellationToken);
    }
}
