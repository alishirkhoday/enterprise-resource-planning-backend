using ERP.Domain.Entities.Modules.ProjectManagement;

namespace ERP.Domain.Repositories.Modules.ProjectManagement;
/// <summary>
/// Projects, their milestones and what has been billed against them - پروژه.
/// </summary>
public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<Project?> GetWithPhasesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetActiveAsync(Guid? managerHumanId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Projects that have spent more than they were budgeted.</summary>
    Task<IReadOnlyList<Project>> GetOverBudgetAsync(CancellationToken cancellationToken = default);

    /// <summary>Milestones reached but not yet invoiced, which is what a billing run picks up.</summary>
    Task<IReadOnlyList<ProjectMilestone>> GetBillableMilestonesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// One task, tracked, for booking time against it. Reached without its project so that filling
    /// in a timesheet does not load a whole work breakdown structure per line.
    /// </summary>
    Task<ProjectTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>One milestone, tracked, for recording that it has been reached.</summary>
    Task<ProjectMilestone?> GetMilestoneAsync(Guid milestoneId, CancellationToken cancellationToken = default);
}
