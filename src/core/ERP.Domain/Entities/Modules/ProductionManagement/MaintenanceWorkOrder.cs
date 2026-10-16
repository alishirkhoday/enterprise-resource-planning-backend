namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Work carried out on a machine, whether planned or after a breakdown, with the parts and
/// labour it consumed.
/// </summary>
public sealed class MaintenanceWorkOrder : BaseEntity
{
    public string Number { get; private set; } = default!;

    public Machine Machine { get; private set; } = default!;
    public Guid MachineId { get; private set; }

    public MaintenanceType Type { get; private set; }
    public MaintenanceWorkOrderStatus Status { get; private set; }
    public MaintenancePriority Priority { get; private set; }

    public Guid? ScheduleId { get; private set; }
    public Guid? DowntimeLogId { get; private set; }

    public DateOnly RequestedOn { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public decimal? EstimatedHours { get; private set; }
    public decimal ActualHours { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public Money PartsCost { get; private set; } = default!;
    public Money LabourCost { get; private set; } = default!;
    public Money TotalCost => PartsCost.Add(LabourCost);

    public Guid? AssignedToHumanId { get; private set; }
    public Guid? CostCenterId { get; private set; }

    public string ProblemDescription { get; private set; } = default!;
    public string? WorkPerformed { get; private set; }

    private readonly List<MaintenancePartUsage> _partsUsed = [];
    public IReadOnlyList<MaintenancePartUsage> PartsUsed => _partsUsed;

    private MaintenanceWorkOrder() { }

    public MaintenanceWorkOrder(string number, Machine machine, MaintenanceType type, DateOnly requestedOn, string problemDescription, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(machine, nameof(machine));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(problemDescription, nameof(problemDescription));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Number = number.Trim();
        Machine = machine;
        MachineId = machine.Id;
        Type = type;
        RequestedOn = requestedOn;
        ProblemDescription = problemDescription.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        PartsCost = Money.Zero(CurrencyCode);
        LabourCost = Money.Zero(CurrencyCode);
        Priority = MaintenancePriority.Normal;
        Status = MaintenanceWorkOrderStatus.Requested;
    }

    public void SetPriority(MaintenancePriority priority) => Priority = priority;

    public void LinkToSchedule(Guid scheduleId) => ScheduleId = scheduleId == Guid.Empty ? null : scheduleId;

    public void LinkToDowntime(Guid downtimeLogId) => DowntimeLogId = downtimeLogId == Guid.Empty ? null : downtimeLogId;

    public void SetEstimate(decimal estimatedHours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(estimatedHours, nameof(estimatedHours));
        EstimatedHours = estimatedHours;
    }

    public void AssignTo(Guid humanId) => AssignedToHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void Approve()
    {
        if (Status != MaintenanceWorkOrderStatus.Requested)
            throw new InvalidOperationException($"A work order in state {Status} cannot be approved.");
        Status = MaintenanceWorkOrderStatus.Approved;
    }

    public void Start()
    {
        if (Status != MaintenanceWorkOrderStatus.Approved)
            throw new InvalidOperationException($"A work order in state {Status} cannot be started.");
        StartedAt = DateTimeOffset.UtcNow;
        Status = MaintenanceWorkOrderStatus.InProgress;
    }

    public void UsePart(MaintenancePartUsage partUsage)
    {
        ArgumentNullException.ThrowIfNull(partUsage, nameof(partUsage));
        if (Status != MaintenanceWorkOrderStatus.InProgress)
            throw new InvalidOperationException("Parts can only be booked while the work is in progress.");
        _partsUsed.Add(partUsage);
        PartsCost = PartsCost.Add(partUsage.TotalCost);
    }

    public void RecordLabour(decimal hours, Money hourlyRate)
    {
        ArgumentNullException.ThrowIfNull(hourlyRate, nameof(hourlyRate));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours, nameof(hours));
        if (Status != MaintenanceWorkOrderStatus.InProgress)
            throw new InvalidOperationException("Labour can only be booked while the work is in progress.");
        ActualHours += hours;
        LabourCost = LabourCost.Add(hourlyRate.Multiply(hours));
    }

    public void Complete(string workPerformed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workPerformed, nameof(workPerformed));
        if (Status != MaintenanceWorkOrderStatus.InProgress)
            throw new InvalidOperationException($"A work order in state {Status} cannot be completed.");
        WorkPerformed = workPerformed.Trim();
        CompletedAt = DateTimeOffset.UtcNow;
        Status = MaintenanceWorkOrderStatus.Completed;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == MaintenanceWorkOrderStatus.Completed)
            throw new InvalidOperationException("A completed work order cannot be cancelled.");
        Status = MaintenanceWorkOrderStatus.Cancelled;
        WorkPerformed = reason.Trim();
    }

    public decimal? HoursVariance => EstimatedHours.HasValue ? ActualHours - EstimatedHours.Value : null;
}
