using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.PricingManagement;

public class PriceListRepository(IMainDbContext context) : Repository<PriceList>(context), IPriceListRepository
{
    public async Task<PriceList?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<PriceList?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// One query rather than three attempts in sequence. The ordering carries the precedence: the
    /// customer's own list first, then their group's, then the default - and among equals, the one
    /// that started most recently, which is the one that was negotiated last.
    /// </summary>
    public async Task<PriceList?> GetEffectiveForSaleAsync(
        Guid? preferredPriceListId,
        Guid? customerGroupId,
        string currencyCode,
        DateOnly onDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();

        return await Query
            .Include(p => p.Items)
            .Where(p => p.IsActive
                        && p.CurrencyCode == normalizedCurrency
                        && p.ValidityPeriod.StartDate <= onDate
                        && (p.ValidityPeriod.EndDate == null || p.ValidityPeriod.EndDate >= onDate)
                        && (p.Id == preferredPriceListId
                            || (customerGroupId != null && p.CustomerGroupId == customerGroupId)
                            || (p.IsDefault && p.CustomerGroupId == null)))
            .OrderByDescending(p => p.Id == preferredPriceListId)
            .ThenByDescending(p => p.CustomerGroupId != null)
            .ThenByDescending(p => p.ValidityPeriod.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceList>> GetActiveOnAsync(DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.IsActive
                        && p.ValidityPeriod.StartDate <= onDate
                        && (p.ValidityPeriod.EndDate == null || p.ValidityPeriod.EndDate >= onDate))
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);
    }
}
