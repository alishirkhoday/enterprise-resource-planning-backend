namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// One routing operation as it is actually carried out on one production order, with the time
/// and output it really took.
/// </summary>
public sealed class ProductionTask : BaseEntity
{
    public ProductionOrder Order { get; private set; } = default!;
    public Guid OrderId { get; private set; }

    public string Title { get; private set; } = default!;
    public int SequenceNumber { get; private set; }

    public WorkCenter WorkCenter { get; private set; } = default!;
    public Guid WorkCenterId { get; private set; }

    public Guid? WorkStationId { get; private set; }
    public Guid? RoutingOperationId { get; private set; }
    public Guid? MachineId { get; private set; }

    public ProductionTaskStatus Status { get; private set; } = ProductionTaskStatus.Pending;

    public DateTimeOffset? PlannedStartTime { get; private set; }
    public DateTimeOffset? PlannedEndTime { get; private set; }
    public DateTimeOffset? StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }

    public decimal PlannedQuantity { get; private set; }
    public decimal CompletedQuantity { get; private set; }
    public decimal ScrappedQuantity { get; private set; }

    public decimal PlannedHours { get; private set; }
    public decimal ActualHours { get; private set; }

    public string? Notes { get; private set; }

    private readonly List<TaskAssignment> _assignments = [];
    public IReadOnlyList<TaskAssignment> Assignments => _assignments;

    private ProductionTask() { }

    public ProductionTask(ProductionOrder order, string title, int sequenceNumber, WorkCenter workCenter, decimal plannedQuantity, decimal plannedHours)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        ArgumentNullException.ThrowIfNull(workCenter, nameof(workCenter));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequenceNumber, nameof(sequenceNumber));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(plannedQuantity, nameof(plannedQuantity));
        ArgumentOutOfRangeException.ThrowIfNegative(plannedHours, nameof(plannedHours));

        Order = order;
        OrderId = order.Id;
        Title = title.Trim();
        SequenceNumber = sequenceNumber;
        WorkCenter = workCenter;
        WorkCenterId = workCenter.Id;
        PlannedQuantity = plannedQuantity;
        PlannedHours = plannedHours;
    }

    public void Schedule(DateTimeOffset plannedStartTime, DateTimeOffset plannedEndTime)
    {
        if (plannedEndTime <= plannedStartTime)
            throw new ArgumentOutOfRangeException(nameof(plannedEndTime), "The end must come after the start.");
        PlannedStartTime = plannedStartTime;
        PlannedEndTime = plannedEndTime;
    }

    public void LinkToRoutingOperation(Guid routingOperationId) =>
        RoutingOperationId = routingOperationId == Guid.Empty ? null : routingOperationId;

    public void AssignResources(Guid? workStationId, Guid? machineId)
    {
        WorkStationId = workStationId == Guid.Empty ? null : workStationId;
        MachineId = machineId == Guid.Empty ? null : machineId;
    }

    public void AddAssignment(TaskAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment, nameof(assignment));
        if (Status is ProductionTaskStatus.Completed or ProductionTaskStatus.Cancelled)
            throw new InvalidOperationException($"People cannot be assigned to a {Status} task.");
        _assignments.Add(assignment);
    }

    public void Start()
    {
        if (Status != ProductionTaskStatus.Pending)
            throw new InvalidOperationException($"A task in state {Status} cannot be started.");
        StartTime = DateTimeOffset.UtcNow;
        Status = ProductionTaskStatus.InProgress;
    }

    public void RecordProgress(decimal completedQuantity, decimal scrappedQuantity = 0m)
    {
        if (Status != ProductionTaskStatus.InProgress)
            throw new InvalidOperationException("Progress can only be recorded while the task is in progress.");
        ArgumentOutOfRangeException.ThrowIfNegative(completedQuantity, nameof(completedQuantity));
        ArgumentOutOfRangeException.ThrowIfNegative(scrappedQuantity, nameof(scrappedQuantity));
        if (CompletedQuantity + completedQuantity > PlannedQuantity)
            throw new InvalidOperationException($"Completing {completedQuantity} would exceed the planned {PlannedQuantity}.");

        CompletedQuantity += completedQuantity;
        ScrappedQuantity += scrappedQuantity;
    }

    public void RecordHours(decimal hours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours, nameof(hours));
        ActualHours += hours;
    }

    public void Pause(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != ProductionTaskStatus.InProgress)
            throw new InvalidOperationException("Only a task in progress can be paused.");
        Status = ProductionTaskStatus.Paused;
        Notes = reason.Trim();
    }

    public void Resume()
    {
        if (Status != ProductionTaskStatus.Paused)
            throw new InvalidOperationException("Only a paused task can be resumed.");
        Status = ProductionTaskStatus.InProgress;
    }

    public void Complete()
    {
        if (Status is not (ProductionTaskStatus.InProgress or ProductionTaskStatus.Paused))
            throw new InvalidOperationException($"A task in state {Status} cannot be completed.");
        EndTime = DateTimeOffset.UtcNow;
        Status = ProductionTaskStatus.Completed;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == ProductionTaskStatus.Completed)
            throw new InvalidOperationException("A completed task cannot be cancelled.");
        Status = ProductionTaskStatus.Cancelled;
        Notes = reason.Trim();
    }

    public void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    /// <summary>Actual hours against planned, which is the efficiency of the step.</summary>
    public decimal? EfficiencyPercentage =>
        ActualHours == 0m ? null : PlannedHours / ActualHours * 100m;

    public decimal RemainingQuantity => PlannedQuantity - CompletedQuantity;

    /// <summary>The labour cost of the hours actually spent, at the work centre rate.</summary>
    public Money? LabourCost() => WorkCenter.CostOf(ActualHours);
}
