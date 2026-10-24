using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Repositories.Modules.ProjectManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.ProjectManagement;

public class ProjectRepository(IMainDbContext context) : Repository<Project>(context), IProjectRepository
{
    public async Task<Project?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<Project?> GetWithPhasesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(p => p.Phases).ThenInclude(phase => phase.Tasks)
            .Include(p => p.Phases).ThenInclude(phase => phase.Milestones)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetActiveAsync(Guid? managerHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.Status == ProjectStatus.Active
                        && (managerHumanId == null || p.ManagerHumanId == managerHumanId))
            .OrderBy(p => p.PlannedPeriod.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.PlannedPeriod.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetOverBudgetAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.ActualCost.Amount > p.BudgetedCost.Amount)
            .OrderByDescending(p => p.ActualCost.Amount - p.BudgetedCost.Amount)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Milestones that have been reached, carry an amount, and have not been invoiced. All three
    /// conditions have to hold before anything can be billed for them.
    /// </summary>
    public async Task<IReadOnlyList<ProjectMilestone>> GetBillableMilestonesAsync(CancellationToken cancellationToken = default)
    {
        var achieved = new[] { MilestoneStatus.Achieved, MilestoneStatus.AchievedLate };

        return await _context.ProjectMilestones
            .AsNoTracking()
            .Where(m => achieved.Contains(m.Status)
                        && m.BillableAmount != null
                        && m.InvoiceId == null)
            .OrderBy(m => m.AchievedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
    }

    public async Task<ProjectMilestone?> GetMilestoneAsync(Guid milestoneId, CancellationToken cancellationToken = default)
    {
        var project = await _entity
            .Include(p => p.Phases).ThenInclude(phase => phase.Milestones)
            .FirstOrDefaultAsync(p => p.Phases.Any(phase => phase.Milestones.Any(m => m.Id == milestoneId)), cancellationToken);
        return project?.Phases.SelectMany(phase => phase.Milestones).SingleOrDefault(m => m.Id == milestoneId);
    }
}
