namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// A stage of a project, carrying its own share of the total work and its own tasks.
/// </summary>
public sealed class ProjectPhase : BaseEntity
{
    public Project Project { get; private set; } = default!;
    public Guid ProjectId { get; private set; }

    public string Name { get; private set; } = default!;
    public int SequenceNumber { get; private set; }
    public DateRange PlannedPeriod { get; private set; } = default!;

    /// <summary>The share of the whole project this phase represents.</summary>
    public Percentage Weight { get; private set; } = default!;

    public Percentage ProgressPercentage { get; private set; } = default!;
    public ProjectPhaseStatus Status { get; private set; }

    public Money BudgetedCost { get; private set; } = default!;
    public Money ActualCost { get; private set; } = default!;

    public DateOnly? ActualStartDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }
    public string? Description { get; private set; }

    private readonly List<ProjectTask> _tasks = [];
    public IReadOnlyList<ProjectTask> Tasks => _tasks;

    private readonly List<ProjectMilestone> _milestones = [];
    public IReadOnlyList<ProjectMilestone> Milestones => _milestones;

    private ProjectPhase() { }

    public ProjectPhase(Project project, string name, int sequenceNumber, DateRange plannedPeriod, Percentage weight)
    {
        ArgumentNullException.ThrowIfNull(project, nameof(project));
        ArgumentNullException.ThrowIfNull(plannedPeriod, nameof(plannedPeriod));
        ArgumentNullException.ThrowIfNull(weight, nameof(weight));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequenceNumber, nameof(sequenceNumber));

        Project = project;
        ProjectId = project.Id;
        Name = name.Trim();
        SequenceNumber = sequenceNumber;
        PlannedPeriod = plannedPeriod;
        Weight = weight;
        ProgressPercentage = Percentage.Zero();
        BudgetedCost = Money.Zero(project.CurrencyCode);
        ActualCost = Money.Zero(project.CurrencyCode);
        Status = ProjectPhaseStatus.NotStarted;
    }

    public void SetBudget(Money budgetedCost)
    {
        ArgumentNullException.ThrowIfNull(budgetedCost, nameof(budgetedCost));
        BudgetedCost = budgetedCost;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddTask(ProjectTask task)
    {
        ArgumentNullException.ThrowIfNull(task, nameof(task));
        _tasks.Add(task);
    }

    public void AddMilestone(ProjectMilestone milestone)
    {
        ArgumentNullException.ThrowIfNull(milestone, nameof(milestone));
        _milestones.Add(milestone);
    }

    public void Start(DateOnly actualStartDate)
    {
        if (Status != ProjectPhaseStatus.NotStarted)
            throw new InvalidOperationException($"A phase in state {Status} cannot be started.");
        ActualStartDate = actualStartDate;
        Status = ProjectPhaseStatus.InProgress;
    }

    public void RecordCost(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        ActualCost = ActualCost.Add(amount);
        Project.RecordCost(amount);
    }

    /// <summary>Recomputes progress from the tasks and rolls it up to the project.</summary>
    public void RecalculateProgress()
    {
        if (_tasks.Count == 0)
            return;
        var average = _tasks.Average(t => t.ProgressPercentage.Value);
        ProgressPercentage = Percentage.Create(Math.Round(average, 2));
        Project.RecalculateProgressFromPhases();
    }

    public void Complete(DateOnly actualEndDate)
    {
        if (Status != ProjectPhaseStatus.InProgress)
            throw new InvalidOperationException($"A phase in state {Status} cannot be completed.");
        ActualEndDate = actualEndDate;
        ProgressPercentage = Percentage.Create(100m);
        Status = ProjectPhaseStatus.Completed;
        Project.RecalculateProgressFromPhases();
    }

    public void Cancel() => Status = ProjectPhaseStatus.Cancelled;
}
