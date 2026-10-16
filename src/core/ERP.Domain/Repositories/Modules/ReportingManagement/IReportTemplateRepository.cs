using ERP.Domain.Entities.Modules.ReportingManagement;

namespace ERP.Domain.Repositories.Modules.ReportingManagement;
/// <summary>
/// Report definitions and their schedules. Definitions only - nothing here runs a report, and
/// nothing in this application does either.
/// </summary>
public interface IReportTemplateRepository : IRepository<ReportTemplate>
{
    Task<ReportTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<ReportTemplate?> GetWithFiltersAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportTemplate>> GetByCategoryAsync(ReportCategory category, CancellationToken cancellationToken = default);

    Task<bool> IsExistenceByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules that are switched on, which is what a dispatcher reads to decide what to send.
    /// </summary>
    Task<IReadOnlyList<ScheduledReport>> GetActiveSchedulesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledReport>> GetSchedulesForTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task AddScheduleAsync(ScheduledReport schedule, CancellationToken cancellationToken = default);
}
