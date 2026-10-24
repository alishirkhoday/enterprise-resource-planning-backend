using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Entities.Modules.ProjectManagement;

public sealed partial class Project
{
    public void UpdateProjectTask(Guid childId, string title, DateRange plannedPeriod, decimal estimatedHours)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.Update(title, plannedPeriod, estimatedHours);
    }

    public void SetProjectTaskDescription(Guid childId, string? description)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.SetDescription(description);
    }

    public void SetProjectTaskPriority(Guid childId, ProjectTaskPriority priority)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.SetPriority(priority);
    }

    public void AssignProjectTaskTo(Guid childId, Guid humanId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.AssignTo(humanId);
    }

    public void DependOnProjectTask(Guid childId, Guid taskId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.DependOn(taskId);
    }

    public void MarkProjectTaskAsNonBillable(Guid childId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.MarkAsNonBillable();
    }

    public void StartProjectTask(Guid childId, DateOnly actualStartDate)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.Start(actualStartDate);
    }

    public void RecordProjectTaskHours(Guid childId, decimal hours)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.RecordHours(hours);
    }

    public void ReportProgressProjectTask(Guid childId, Percentage progressPercentage)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.ReportProgress(progressPercentage);
    }

    public void BlockProjectTask(Guid childId, string reason)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.Block(reason);
    }

    public void UnblockProjectTask(Guid childId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.Unblock();
    }

    public void CompleteProjectTask(Guid childId, DateOnly actualEndDate)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.Complete(actualEndDate);
    }

    public void CancelProjectTask(Guid childId, string reason)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Tasks));
        child.Cancel(reason);
    }

    public void SetProjectMilestoneBillableAmount(Guid childId, Money billableAmount)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Milestones));
        child.SetBillableAmount(billableAmount);
    }

    public void SetProjectMilestoneAcceptanceCriteria(Guid childId, string? criteria)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Milestones));
        child.SetAcceptanceCriteria(criteria);
    }

    public void SetProjectMilestoneDescription(Guid childId, string? description)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Milestones));
        child.SetDescription(description);
    }

    public void AchieveProjectMilestone(Guid childId, DateOnly achievedOn)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Milestones));
        child.Achieve(achievedOn);
    }

    public void BillProjectMilestone(Guid childId, Guid invoiceId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Milestones));
        child.Bill(invoiceId);
    }

    public void MissProjectMilestone(Guid childId, string reason)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases.SelectMany(parent => parent.Milestones));
        child.Miss(reason);
    }

    public void SetProjectPhaseBudget(Guid childId, Money budgetedCost)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases);
        child.SetBudget(budgetedCost);
    }

    public void SetProjectPhaseDescription(Guid childId, string? description)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases);
        child.SetDescription(description);
    }

    public void AddProjectPhaseTask(Guid childId, ProjectTask task)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases);
        child.AddTask(task);
    }

    public void AddProjectPhaseMilestone(Guid childId, ProjectMilestone milestone)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases);
        child.AddMilestone(milestone);
    }

    public void StartProjectPhase(Guid childId, DateOnly actualStartDate)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases);
        child.Start(actualStartDate);
    }

    public void RecordProjectPhaseCost(Guid childId, Money amount)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases);
        child.RecordCost(amount);
    }

    public void RecalculateProgressProjectPhase(Guid childId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases);
        child.RecalculateProgress();
    }

    public void CompleteProjectPhase(Guid childId, DateOnly actualEndDate)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases);
        child.Complete(actualEndDate);
    }

    public void CancelProjectPhase(Guid childId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _phases);
        child.Cancel();
    }

    public void SetProjectMemberRates(Guid childId, Money? hourlyCostRate, Money? hourlyBillingRate)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _members);
        child.SetRates(hourlyCostRate, hourlyBillingRate);
    }

    public void ChangeProjectMemberAllocation(Guid childId, Percentage allocation)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _members);
        child.ChangeAllocation(allocation);
    }

    public void LeaveProjectMember(Guid childId, DateOnly leftOn)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _members);
        child.Leave(leftOn);
    }
    private void EnsureChildChangesAllowed()
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
            throw new InvalidOperationException("A completed or cancelled aggregate cannot change its children.");
    }
}
