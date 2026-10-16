using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.AfterSalesServiceManagement;

public class ServiceContractRepository(IMainDbContext context) : Repository<ServiceContract>(context), IServiceContractRepository
{
    public async Task<ServiceContract?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(c => c.Number == number, cancellationToken);
    }

    public async Task<ServiceContract?> GetWithCoverageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Customer)
            .Include(c => c.CoveredAssets).ThenInclude(a => a.Item)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Tracked, because the caller is usually about to raise a service order under this contract
    /// and record the visit against it.
    /// <para>
    /// The ordering is the rule: a contract naming this exact serial number is the one that was
    /// negotiated for the unit, so it beats a blanket contract that merely covers the item.
    /// </para>
    /// </summary>
    public async Task<ServiceContract?> GetCoveringAsync(Guid customerId, Guid itemId, Guid? serialNumberId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.CoveredAssets)
            .Where(c => c.CustomerId == customerId
                        && c.Status == ServiceContractStatus.Active
                        && c.Period.StartDate <= onDate
                        && (c.Period.EndDate == null || c.Period.EndDate >= onDate)
                        && c.CoveredAssets.Any(a => a.ItemId == itemId
                                                    && (a.SerialNumberId == null || a.SerialNumberId == serialNumberId)))
            .OrderByDescending(c => c.CoveredAssets.Any(a => a.ItemId == itemId && a.SerialNumberId == serialNumberId))
            .ThenByDescending(c => c.Period.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceContract>> GetActiveForCustomerAsync(Guid customerId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.CustomerId == customerId
                        && c.Status == ServiceContractStatus.Active
                        && c.Period.StartDate <= onDate
                        && (c.Period.EndDate == null || c.Period.EndDate >= onDate))
            .OrderBy(c => c.Period.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceContract>> GetDueForRenewalAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Customer)
            .Where(c => c.Status == ServiceContractStatus.Active
                        && c.AutoRenews
                        && c.RenewalNoticeDays != null
                        && c.Period.EndDate != null
                        && c.Period.EndDate.Value.DayNumber - asOf.DayNumber <= c.RenewalNoticeDays)
            .OrderBy(c => c.Period.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceContract>> GetWithExhaustedVisitsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Customer)
            .Where(c => c.Status == ServiceContractStatus.Active
                        && c.IncludedVisits != null
                        && c.ConsumedVisits >= c.IncludedVisits)
            .OrderBy(c => c.Period.EndDate)
            .ToListAsync(cancellationToken);
    }
}
