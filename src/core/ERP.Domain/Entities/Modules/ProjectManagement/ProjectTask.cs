namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// A unit of work inside a phase, with the hours it was estimated at and the hours actually
/// booked against it through timesheets.
/// </summary>
public sealed class ProjectTask : BaseEntity
{
    public ProjectPhase Phase { get; private set; } = default!;
    public Guid PhaseId { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public ProjectTaskPriority Priority { get; private set; }

    public DateRange PlannedPeriod { get; private set; } = default!;
    public DateOnly? ActualStartDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }

    public decimal EstimatedHours { get; private set; }
    public decimal ActualHours { get; private set; }
    public Percentage ProgressPercentage { get; private set; } = default!;

    public Guid? AssignedToHumanId { get; private set; }

    /// <summary>The task that must finish before this one can start.</summary>
    public Guid? DependsOnTaskId { get; private set; }

    /// <summary>Whether the task is chargeable to the customer.</summary>
    public bool IsBillable { get; private set; }

    private ProjectTask() { }

    public ProjectTask(ProjectPhase phase, string title, DateRange plannedPeriod, decimal estimatedHours)
    {
        ArgumentNullException.ThrowIfNull(phase, nameof(phase));
        ArgumentNullException.ThrowIfNull(plannedPeriod, nameof(plannedPeriod));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegative(estimatedHours, nameof(estimatedHours));

        Phase = phase;
        PhaseId = phase.Id;
        ProjectId = phase.ProjectId;
        Title = title.Trim();
        PlannedPeriod = plannedPeriod;
        EstimatedHours = estimatedHours;
        ProgressPercentage = Percentage.Zero();
        Status = ProjectTaskStatus.NotStarted;
        Priority = ProjectTaskPriority.Normal;
        IsBillable = true;
    }

    public void Update(string title, DateRange plannedPeriod, decimal estimatedHours)
    {
        ArgumentNullException.ThrowIfNull(plannedPeriod, nameof(plannedPeriod));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegative(estimatedHours, nameof(estimatedHours));
        Title = title.Trim();
        PlannedPeriod = plannedPeriod;
        EstimatedHours = estimatedHours;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void SetPriority(ProjectTaskPriority priority) => Priority = priority;

    public void AssignTo(Guid humanId) => AssignedToHumanId = humanId == Guid.Empty ? null : humanId;

    public void DependOn(Guid taskId)
    {
        if (taskId == Id)
            throw new InvalidOperationException("A task cannot depend on itself.");
        DependsOnTaskId = taskId == Guid.Empty ? null : taskId;
    }

    public void MarkAsNonBillable() => IsBillable = false;

    public void Start(DateOnly actualStartDate)
    {
        if (Status != ProjectTaskStatus.NotStarted)
            throw new InvalidOperationException($"A task in state {Status} cannot be started.");
        ActualStartDate = actualStartDate;
        Status = ProjectTaskStatus.InProgress;
    }

    public void RecordHours(decimal hours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours, nameof(hours));
        ActualHours += hours;
    }

    public void ReportProgress(Percentage progressPercentage)
    {
        ArgumentNullException.ThrowIfNull(progressPercentage, nameof(progressPercentage));
        if (Status is ProjectTaskStatus.Completed or ProjectTaskStatus.Cancelled)
            throw new InvalidOperationException($"Progress cannot be reported on a {Status} task.");
        ProgressPercentage = progressPercentage;
        Phase.RecalculateProgress();
    }

    public void Block(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != ProjectTaskStatus.InProgress)
            throw new InvalidOperationException("Only a task in progress can be blocked.");
        Status = ProjectTaskStatus.Blocked;
        Description = reason.Trim();
    }

    public void Unblock()
    {
        if (Status != ProjectTaskStatus.Blocked)
            throw new InvalidOperationException("Only a blocked task can be unblocked.");
        Status = ProjectTaskStatus.InProgress;
    }

    public void Complete(DateOnly actualEndDate)
    {
        if (Status is not (ProjectTaskStatus.InProgress or ProjectTaskStatus.Blocked))
            throw new InvalidOperationException($"A task in state {Status} cannot be completed.");
        ActualEndDate = actualEndDate;
        ProgressPercentage = Percentage.Create(100m);
        Status = ProjectTaskStatus.Completed;
        Phase.RecalculateProgress();
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == ProjectTaskStatus.Completed)
            throw new InvalidOperationException("A completed task cannot be cancelled.");
        Status = ProjectTaskStatus.Cancelled;
        Description = reason.Trim();
    }

    public decimal HoursVariance => ActualHours - EstimatedHours;

    public bool IsOverEstimate => ActualHours > EstimatedHours;

    public bool IsOverdue(DateOnly asOf) =>
        Status is not (ProjectTaskStatus.Completed or ProjectTaskStatus.Cancelled)
        && PlannedPeriod.EndDate.HasValue
        && asOf > PlannedPeriod.EndDate.Value;
}
