namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// A point the project has to reach, often the trigger for billing a stage of the contract.
/// </summary>
public sealed class ProjectMilestone : BaseEntity
{
    public ProjectPhase Phase { get; private set; } = default!;
    public Guid PhaseId { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Title { get; private set; } = default!;
    public DateOnly DueDate { get; private set; }
    public DateOnly? AchievedOn { get; private set; }
    public MilestoneStatus Status { get; private set; }

    /// <summary>The amount that becomes billable when the milestone is reached.</summary>
    public Money? BillableAmount { get; private set; }
    public Guid? InvoiceId { get; private set; }

    public string? Description { get; private set; }
    public string? AcceptanceCriteria { get; private set; }

    private ProjectMilestone() { }

    public ProjectMilestone(ProjectPhase phase, string title, DateOnly dueDate)
    {
        ArgumentNullException.ThrowIfNull(phase, nameof(phase));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        Phase = phase;
        PhaseId = phase.Id;
        ProjectId = phase.ProjectId;
        Title = title.Trim();
        DueDate = dueDate;
        Status = MilestoneStatus.Pending;
    }

    public void SetBillableAmount(Money billableAmount)
    {
        ArgumentNullException.ThrowIfNull(billableAmount, nameof(billableAmount));
        if (billableAmount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(billableAmount), "A billable amount cannot be negative.");
        BillableAmount = billableAmount;
    }

    public void SetAcceptanceCriteria(string? criteria) =>
        AcceptanceCriteria = string.IsNullOrWhiteSpace(criteria) ? null : criteria.Trim();

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Achieve(DateOnly achievedOn)
    {
        if (Status != MilestoneStatus.Pending)
            throw new InvalidOperationException($"A milestone in state {Status} cannot be achieved.");
        AchievedOn = achievedOn;
        Status = achievedOn > DueDate ? MilestoneStatus.AchievedLate : MilestoneStatus.Achieved;
        Raise(new ProjectMilestoneAchieved(Id, ProjectId, achievedOn));
    }

    public void Bill(Guid invoiceId)
    {
        if (Status is not (MilestoneStatus.Achieved or MilestoneStatus.AchievedLate))
            throw new InvalidOperationException("Only an achieved milestone can be billed.");
        if (BillableAmount is null)
            throw new InvalidOperationException("This milestone carries no billable amount.");
        InvoiceId = invoiceId == Guid.Empty ? null : invoiceId;
        Status = MilestoneStatus.Billed;
    }

    public void Miss(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != MilestoneStatus.Pending)
            throw new InvalidOperationException($"A milestone in state {Status} cannot be missed.");
        Status = MilestoneStatus.Missed;
        Description = reason.Trim();
    }

    public bool IsOverdue(DateOnly asOf) => Status == MilestoneStatus.Pending && asOf > DueDate;
}
