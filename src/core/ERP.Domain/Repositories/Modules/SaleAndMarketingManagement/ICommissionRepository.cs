using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
/// <summary>
/// Sales commission - پورسانت. The lookup by source document is the important one: paying
/// twice on one invoice is the mistake nobody notices until the salesperson does.
/// </summary>
public interface ICommissionRepository : IRepository<Commission>
{
    /// <summary>
    /// Whether commission was already earned on this document. Paying twice on one invoice is the
    /// mistake nobody notices until the salesperson does, so the check is a lookup, not a habit.
    /// </summary>
    Task<Commission?> GetBySourceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Commission>> GetBySalesRepAsync(Guid salesRepHumanId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commission approved and not yet paid. This is what a payroll run picks up, which is why
    /// accrued ones are left out: they are not owed until somebody approves them.
    /// </summary>
    Task<IReadOnlyList<Commission>> GetPayableAsync(Guid? salesRepHumanId = null, CancellationToken cancellationToken = default);

    /// <summary>What a salesperson has earned in a period, summed in the database.</summary>
    Task<decimal> GetEarnedTotalAsync(Guid salesRepHumanId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
