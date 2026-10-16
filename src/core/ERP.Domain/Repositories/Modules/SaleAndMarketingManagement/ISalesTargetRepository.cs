using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
/// <summary>
/// Quotas - هدف فروش. Targets overlap on purpose, so the period is always part of the question.
/// </summary>
public interface ISalesTargetRepository : IRepository<SalesTarget>
{
    /// <summary>
    /// The target that applies to one person, team or branch on a date. Targets overlap in
    /// practice - a quarterly one inside an annual one - so the period is part of the question.
    /// </summary>
    Task<IReadOnlyList<SalesTarget>> GetForScopeAsync(SalesTargetScope scope, Guid scopeReferenceId, DateOnly onDate, CancellationToken cancellationToken = default);

    /// <summary>Every target whose period covers a date, which is what a scoreboard reads.</summary>
    Task<IReadOnlyList<SalesTarget>> GetActiveOnAsync(DateOnly onDate, CancellationToken cancellationToken = default);
}
