using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Repositories.Modules.ProjectManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.ProjectManagement;

public class TimesheetRepository(IMainDbContext context) : Repository<Timesheet>(context), ITimesheetRepository
{
    public async Task<Timesheet?> GetWithEntriesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(t => t.Human)
            .Include(t => t.Entries).ThenInclude(e => e.ProjectTask)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Timesheet?> GetByHumanAndPeriodAsync(Guid humanId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(
                t => t.HumanId == humanId && t.Period.StartDate == startDate && t.Period.EndDate == endDate,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Timesheet>> GetAwaitingApprovalAsync(Guid? approverHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(t => t.Human)
            .Where(t => t.Status == TimesheetStatus.Submitted
                        && (approverHumanId == null || t.Human.ManagerId == approverHumanId))
            .OrderBy(t => t.SubmittedOn)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Only approved sheets count. A draft is someone's private note about their week, and letting
    /// it into a project cost would report work nobody has signed off.
    /// </summary>
    public async Task<decimal> GetApprovedHoursForProjectAsync(Guid projectId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await _context.TimesheetEntries
            .AsNoTracking()
            .Where(e => e.ProjectId == projectId
                        && e.WorkDate >= from
                        && e.WorkDate <= to
                        && e.Timesheet.Status == TimesheetStatus.Approved)
            .SumAsync(e => e.Hours, cancellationToken);
    }

    public async Task<decimal> GetApprovedBillableHoursForProjectAsync(Guid projectId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await _context.TimesheetEntries
            .AsNoTracking()
            .Where(e => e.ProjectId == projectId
                        && e.IsBillable
                        && e.WorkDate >= from
                        && e.WorkDate <= to
                        && e.Timesheet.Status == TimesheetStatus.Approved)
            .SumAsync(e => e.Hours, cancellationToken);
    }
}
