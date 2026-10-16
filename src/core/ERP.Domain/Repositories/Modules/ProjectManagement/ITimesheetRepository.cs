using ERP.Domain.Entities.Modules.ProjectManagement;

namespace ERP.Domain.Repositories.Modules.ProjectManagement;
/// <summary>
/// Time booked to projects - برگه زمانی. Only approved hours reach a project statement, so
/// approval status is part of nearly every question asked here.
/// </summary>
public interface ITimesheetRepository : IRepository<Timesheet>
{
    Task<Timesheet?> GetWithEntriesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Timesheet?> GetByHumanAndPeriodAsync(Guid humanId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>Sheets waiting on a manager, which is the approver queue.</summary>
    Task<IReadOnlyList<Timesheet>> GetAwaitingApprovalAsync(Guid? approverHumanId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approved hours booked to a project in a period. Only approved hours count, so a draft sheet
    /// never inflates what a project appears to have cost.
    /// </summary>
    Task<decimal> GetApprovedHoursForProjectAsync(Guid projectId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<decimal> GetApprovedBillableHoursForProjectAsync(Guid projectId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
