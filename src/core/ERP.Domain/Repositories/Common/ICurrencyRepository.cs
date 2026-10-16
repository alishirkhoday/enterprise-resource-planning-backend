using ERP.Domain.Entities.Common;

namespace ERP.Domain.Repositories.Common;
/// <summary>
/// The currencies the system is allowed to hold money in. Small and slow-changing, but every
/// Money in the system quietly depends on one of these rows existing.
/// </summary>
public interface ICurrencyRepository : IRepository<Currency>
{
    Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>The currency the books are kept in, which every report is expressed in.</summary>
    Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Currency>> GetActiveAsync(CancellationToken cancellationToken = default);
}
