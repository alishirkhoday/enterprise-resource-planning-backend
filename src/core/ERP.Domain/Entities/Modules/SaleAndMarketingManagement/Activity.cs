namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Something someone has to do, or has done, about a customer, a lead or a deal. It is the
/// shared to do list of the sales side, which lead interactions alone could not cover.
/// </summary>
public sealed class Activity : BaseEntity, IBranchScoped
{
    public string Subject { get; private set; } = default!;
    public ActivityType Type { get; private set; }
    public ActivityStatus Status { get; private set; }
    public ActivityPriority Priority { get; private set; }

    /// <summary>What the activity is about, for example Customer or Opportunity.</summary>
    public string ReferenceType { get; private set; } = default!;
    public string ReferenceId { get; private set; } = default!;

    public Guid? AssignedToHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int? DurationMinutes { get; private set; }

    /// <summary>When a reminder should be raised before the activity is due.</summary>
    public DateTimeOffset? RemindAt { get; private set; }

    public string? Description { get; private set; }
    public string? Outcome { get; private set; }

    private Activity() { }

    public Activity(string subject, ActivityType type, string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject, nameof(subject));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));

        Subject = subject.Trim();
        Type = type;
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
        Status = ActivityStatus.Planned;
        Priority = ActivityPriority.Normal;
    }

    public void Schedule(DateTimeOffset dueAt, DateTimeOffset? remindAt = null)
    {
        EnsureOpen();
        if (remindAt.HasValue && remindAt.Value > dueAt)
            throw new ArgumentOutOfRangeException(nameof(remindAt), "A reminder cannot fall after the activity is due.");
        DueAt = dueAt;
        RemindAt = remindAt;
    }

    public void SetPriority(ActivityPriority priority) => Priority = priority;

    public void AssignTo(Guid humanId) => AssignedToHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Start()
    {
        EnsureOpen();
        StartedAt = DateTimeOffset.UtcNow;
        Status = ActivityStatus.InProgress;
    }

    public void Complete(string? outcome = null, int? durationMinutes = null)
    {
        EnsureOpen();
        if (durationMinutes.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationMinutes.Value, nameof(durationMinutes));
        CompletedAt = DateTimeOffset.UtcNow;
        DurationMinutes = durationMinutes;
        Outcome = string.IsNullOrWhiteSpace(outcome) ? null : outcome.Trim();
        Status = ActivityStatus.Completed;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        EnsureOpen();
        Status = ActivityStatus.Cancelled;
        Outcome = reason.Trim();
    }

    public void Reschedule(DateTimeOffset dueAt)
    {
        EnsureOpen();
        DueAt = dueAt;
        Status = ActivityStatus.Planned;
    }

    public bool IsOverdue => Status is ActivityStatus.Planned or ActivityStatus.InProgress
                             && DueAt.HasValue
                             && DueAt.Value < DateTimeOffset.UtcNow;

    private void EnsureOpen()
    {
        if (Status is ActivityStatus.Completed or ActivityStatus.Cancelled)
            throw new InvalidOperationException($"The activity is {Status} and is no longer open.");
    }
}
