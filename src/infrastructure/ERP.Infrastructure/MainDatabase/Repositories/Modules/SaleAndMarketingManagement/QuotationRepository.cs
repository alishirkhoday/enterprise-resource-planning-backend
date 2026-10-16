using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.SaleAndMarketingManagement;

public class QuotationRepository(IMainDbContext context) : Repository<Quotation>(context), IQuotationRepository
{
    public async Task<Quotation?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        // A number can carry several revisions, so the newest one is what a caller asking by
        // number means: the earlier versions were superseded on purpose.
        return await _entity
            .Where(q => q.Number == number)
            .OrderByDescending(q => q.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Quotation?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(q => q.Customer)
            .Include(q => q.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Quotation>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(q => q.CustomerId == customerId)
            .OrderByDescending(q => q.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Quotation>> GetByOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(q => q.OpportunityId == opportunityId)
            .OrderByDescending(q => q.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Quotation>> GetAwaitingDecisionAsync(Guid? salesRepHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(q => q.Customer)
            .Where(q => q.Status == QuotationStatus.Sent
                        && (salesRepHumanId == null || q.SalesRepHumanId == salesRepHumanId))
            // Soonest to expire first, because that is the one where following up still changes
            // the outcome.
            .OrderBy(q => q.ExpiryDate ?? DateTimeOffset.MaxValue)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Quotation>> GetExpiredAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(q => q.Status == QuotationStatus.Sent && q.ExpiryDate != null && q.ExpiryDate < asOf)
            .OrderBy(q => q.ExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetLatestVersionAsync(string number, CancellationToken cancellationToken = default)
    {
        var versions = await Query
            .Where(q => q.Number == number)
            .Select(q => (int?)q.Version)
            .ToListAsync(cancellationToken);

        return versions.Count == 0 ? 0 : versions.Max() ?? 0;
    }
}
