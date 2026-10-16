using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Repositories.Modules.HumanResourcesManagement;

/// <summary>
/// Employment contracts - حکم کارگزینی. Written, amended and ended on their own schedule, which
/// is why they are not loaded through the person: issuing the next contract should not mean
/// pulling somebody's whole employment history.
/// </summary>
public interface IEmploymentContractRepository : IRepository<HumanContract>
{
    /// <summary>The contract in force on a date, which payroll reads to value a period.</summary>
    Task<HumanContract?> GetEffectiveAsync(Guid humanId, DateOnly onDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HumanContract>> GetForHumanAsync(Guid humanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Contracts ending inside a window. A fixed term that lapses without anybody noticing leaves
    /// somebody working with no agreement behind them, which is the one thing nobody spots until
    /// payroll fails.
    /// </summary>
    Task<IReadOnlyList<HumanContract>> GetExpiringBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task AddAsync(HumanContract contract, CancellationToken cancellationToken = default);
}
