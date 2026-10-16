using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Common;
using ERP.Domain.Repositories.Common;

namespace ERP.Infrastructure.MainDatabase.Repositories.Common;

public class ExchangeRateRepository(IMainDbContext context) : Repository<ExchangeRate>(context), IExchangeRateRepository
{
    /// <summary>
    /// The most recent rate published on or before the date. Rates are not published every day, so
    /// asking for an exact match would leave most days without one.
    /// </summary>
    public async Task<ExchangeRate?> GetRateOnAsync(Guid fromCurrencyId, Guid toCurrencyId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(r => r.FromCurrencyId == fromCurrencyId
                        && r.ToCurrencyId == toCurrencyId
                        && r.EffectiveDate <= date)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExchangeRate?> GetLatestAsync(Guid fromCurrencyId, Guid toCurrencyId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(r => r.FromCurrencyId == fromCurrencyId && r.ToCurrencyId == toCurrencyId)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(Guid fromCurrencyId, Guid toCurrencyId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(r => r.FromCurrencyId == fromCurrencyId
                        && r.ToCurrencyId == toCurrencyId
                        && r.EffectiveDate >= from
                        && r.EffectiveDate <= to)
            .OrderBy(r => r.EffectiveDate)
            .ToListAsync(cancellationToken);
    }
}
