using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// Financial years and their periods - سال مالی. What decides whether a posting is allowed at
/// all, since a closed period takes nothing.
/// </summary>
public interface IFinancialYearRepository : IRepository<FinancialYear>
{
    /// <summary>
    /// The year a given date falls in, together with its periods. Every posting needs this to know
    /// which book it belongs to and whether that book is still open.
    /// </summary>
    Task<FinancialYear?> GetByDateAsync(DateOnly date, Guid? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>The year currently accepting postings.</summary>
    Task<FinancialYear?> GetOpenYearAsync(Guid? branchId = null, CancellationToken cancellationToken = default);

    Task<FinancialYear?> GetWithPeriodsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The period a date falls in, which is what a posting is actually blocked by.</summary>
    Task<AccountingPeriod?> GetPeriodByDateAsync(DateOnly date, Guid? branchId = null, CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingYearAsync(DateOnly startDate, DateOnly endDate, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
