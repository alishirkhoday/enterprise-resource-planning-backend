using ERP.Domain.Entities.Common;

namespace ERP.Domain.Repositories.Common;
/// <summary>
/// The counters that hand out document numbers - invoices, orders, letters. Every series is
/// read for update rather than read plainly: two callers must never be handed the same number.
/// </summary>
public interface INumberingSeriesRepository : IRepository<NumberingSeries>
{
    /// <summary>
    /// Loads the series tracked so the next number can be consumed and saved.
    /// <para>
    /// Two requests asking for an invoice number at the same instant must not be handed the same
    /// one. The series carries a concurrency stamp that rotates on every change, so the second
    /// writer is rejected with a concurrency error rather than silently overwriting the first.
    /// The caller is expected to retry, which re-reads the counter.
    /// </para>
    /// </summary>
    Task<NumberingSeries?> GetForUpdateAsync(string key, Guid? branchId = null, Guid? financialYearId = null, CancellationToken cancellationToken = default);

    Task<NumberingSeries?> GetByKeyAsync(string key, Guid? branchId = null, Guid? financialYearId = null, CancellationToken cancellationToken = default);
}
