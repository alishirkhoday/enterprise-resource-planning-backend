using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.SaleAndMarketingManagement;

public class CampaignRepository(IMainDbContext context) : Repository<Campaign>(context), ICampaignRepository
{
    public async Task<Campaign?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    public async Task<Campaign?> GetWithLeadsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Leads)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// An open ended campaign - one with no end date - keeps running, which is why the end is
    /// compared only when there is one.
    /// </summary>
    public async Task<IReadOnlyList<Campaign>> GetRunningOnAsync(DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.Status == CampaignStatus.Running
                        && c.Period.StartDate <= onDate
                        && (c.Period.EndDate == null || c.Period.EndDate >= onDate))
            .OrderBy(c => c.Period.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistenceByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(c => c.Code == code && (excludeId == null || c.Id != excludeId), cancellationToken);
    }
}
