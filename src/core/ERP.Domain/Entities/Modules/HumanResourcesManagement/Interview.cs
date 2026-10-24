namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// An interview with a candidate - مصاحبه.
/// </summary>
public sealed class Interview : BaseEntity
{
    public JobApplication Application { get; private set; } = default!;
    public Guid ApplicationId { get; private set; }

    public InterviewType Type { get; private set; }
    public int Round { get; private set; }
    public DateTimeOffset ScheduledFor { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Location { get; private set; }

    public Guid? InterviewerHumanId { get; private set; }
    public InterviewResult? Result { get; private set; }
    public int? Score { get; private set; }
    public string? Feedback { get; private set; }
    public DateTimeOffset? HeldOn { get; private set; }

    private Interview() { }

    public Interview(JobApplication application, InterviewType type, int round, DateTimeOffset scheduledFor, int durationMinutes = 60)
    {
        ArgumentNullException.ThrowIfNull(application, nameof(application));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(round, nameof(round));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationMinutes, nameof(durationMinutes));

        Application = application;
        ApplicationId = application.Id;
        Type = type;
        Round = round;
        ScheduledFor = scheduledFor;
        DurationMinutes = durationMinutes;
    }

    internal void SetLocation(string? location) => Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();

    internal void AssignInterviewer(Guid humanId) => InterviewerHumanId = humanId == Guid.Empty ? null : humanId;

    internal void Reschedule(DateTimeOffset scheduledFor)
    {
        if (Result is not null)
            throw new InvalidOperationException("A completed interview cannot be rescheduled.");
        ScheduledFor = scheduledFor;
    }

    internal void RecordResult(InterviewResult result, int? score, string? feedback)
    {
        if (score.HasValue && (score.Value < 0 || score.Value > 100))
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0 and 100.");
        Result = result;
        Score = score;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        HeldOn = DateTimeOffset.UtcNow;
    }
}
