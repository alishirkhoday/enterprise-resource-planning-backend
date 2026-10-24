using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// One employee working one task on one shift. It points at the employee record rather than
/// repeating their details, so the shop floor and human resources agree on who is who.
/// </summary>
public sealed class TaskAssignment : BaseEntity
{
    public ProductionTask Task { get; private set; } = default!;
    public Guid TaskId { get; private set; }

    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public Shift Shift { get; private set; } = default!;
    public Guid ShiftId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateOnly WorkDate { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>Hours the employee actually spent on the task.</summary>
    public decimal Hours { get; private set; }

    public string? Notes { get; private set; }

    private TaskAssignment() { }

    public TaskAssignment(ProductionTask task, Human human, Shift shift, DateOnly workDate)
    {
        ArgumentNullException.ThrowIfNull(task, nameof(task));
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(shift, nameof(shift));
        if (human.Status != HumanStatus.Active)
            throw new InvalidOperationException($"Employee {human.FullName} is {human.Status} and cannot be assigned.");

        Task = task;
        TaskId = task.Id;
        Human = human;
        HumanId = human.Id;
        Shift = shift;
        ShiftId = shift.Id;
        WorkDate = workDate;
    }

    internal void Start()
    {
        if (StartedAt is not null)
            throw new InvalidOperationException("The assignment has already started.");
        StartedAt = DateTimeOffset.UtcNow;
    }

    internal void End(decimal hours)
    {
        if (StartedAt is null)
            throw new InvalidOperationException("The assignment has not started.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours, nameof(hours));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hours, 24m, nameof(hours));
        EndedAt = DateTimeOffset.UtcNow;
        Hours = hours;
        Task.RecordHours(hours);
    }

    internal void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
