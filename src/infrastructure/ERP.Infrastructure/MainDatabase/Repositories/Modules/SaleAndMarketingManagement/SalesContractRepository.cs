using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.SaleAndMarketingManagement;

public class SalesContractRepository(IMainDbContext context) : Repository<SalesContract>(context), ISalesContractRepository
{
    public async Task<SalesContract?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(c => c.Number == number, cancellationToken);
    }

    /// <summary>
    /// The most recently signed contract that covers the date wins. Customers do sign a
    /// replacement before the old one lapses, and the newer terms are the ones that apply.
    /// </summary>
    public async Task<SalesContract?> GetEffectiveForCustomerAsync(Guid customerId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.CustomerId == customerId
                        && c.Status == SalesContractStatus.Active
                        && c.Period.StartDate <= onDate
                        && (c.Period.EndDate == null || c.Period.EndDate >= onDate))
            .OrderByDescending(c => c.SignedOn ?? c.Period.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Inside the notice period and not yet dealt with. A contract with no end date never comes
    /// up for renewal, and one with no notice period is given thirty days by convention.
    /// </summary>
    public async Task<IReadOnlyList<SalesContract>> GetDueForRenewalAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        const int DefaultNoticeDays = 30;

        return await Query
            .Include(c => c.Customer)
            .Where(c => c.Status == SalesContractStatus.Active
                        && c.Period.EndDate != null
                        && c.Period.EndDate >= asOf
                        && c.Period.EndDate <= asOf.AddDays(c.RenewalNoticeDays ?? DefaultNoticeDays))
            .OrderBy(c => c.Period.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SalesContract>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.Period.StartDate)
            .ToListAsync(cancellationToken);
    }
}
