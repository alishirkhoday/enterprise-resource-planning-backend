using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.SaleAndMarketingManagement;

public class LeadRepository(IMainDbContext context) : Repository<Lead>(context), ILeadRepository
{
    private static readonly LeadStatus[] OpenStatuses =
    [
        LeadStatus.New,
        LeadStatus.Contacted,
        LeadStatus.Qualified
    ];

    public async Task<Lead?> GetWithHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(l => l.Notes)
            .Include(l => l.Interactions)
            .Include(l => l.Opportunities)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Lead>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(l => OpenStatuses.Contains(l.Status)
                        && (assignedToHumanId == null || l.AssignedToHumanId == assignedToHumanId))
            // Highest score first: a worklist is only useful if the best lead is at the top, and
            // a lead nobody scored sorts below one somebody did.
            .OrderByDescending(l => l.Score ?? 0)
            .ThenBy(l => l.CreationDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lead>> GetByCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(l => l.CampaignId == campaignId)
            .OrderByDescending(l => l.CreationDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lead?> GetByMobileNumberAsync(string mobilePhoneNumberWithRegionCode, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Where(l => l.MobilePhoneNumberWithRegionCode == mobilePhoneNumberWithRegionCode)
            .OrderByDescending(l => l.CreationDateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Both numbers come back from one pass over the campaign's leads. Counting them separately
    /// would let the two disagree if a lead converted between the queries.
    /// </summary>
    public async Task<(int Total, int Converted)> GetCampaignConversionAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var counted = await Query
            .Where(l => l.CampaignId == campaignId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Converted = g.Count(l => l.Status == LeadStatus.Converted)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return counted is null ? (0, 0) : (counted.Total, counted.Converted);
    }
}
