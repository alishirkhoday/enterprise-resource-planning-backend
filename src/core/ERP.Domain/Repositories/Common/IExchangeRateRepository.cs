using ERP.Domain.Entities.Common;

namespace ERP.Domain.Repositories.Common;
/// <summary>
/// Rates between currencies on a date. Rates are read by date rather than "latest", because a
/// document has to keep being worth what it was worth when it was raised.
/// </summary>
public interface IExchangeRateRepository : IRepository<ExchangeRate>
{
    /// <summary>
    /// The rate in force on a date: the latest one published on or before it. Rates are history,
    /// so revaluing an old document uses the rate that applied then, not today's.
    /// </summary>
    Task<ExchangeRate?> GetRateOnAsync(Guid fromCurrencyId, Guid toCurrencyId, DateOnly date, CancellationToken cancellationToken = default);

    Task<ExchangeRate?> GetLatestAsync(Guid fromCurrencyId, Guid toCurrencyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(Guid fromCurrencyId, Guid toCurrencyId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
