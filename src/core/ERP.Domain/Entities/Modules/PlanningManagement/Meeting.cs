namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// A meeting - جلسه. Its minutes and decisions, so a commitment made in a room can be traced
/// later.
/// </summary>
public sealed class Meeting : BaseEntity
{
    public string Title { get; private set; } = default!;
    public MeetingType Type { get; private set; } = default!;
    public DateTimeOffset StartDateTime { get; private set; }
    public int Time { get; private set; }
    public string Location { get; private set; } = default!;
    public string? Decisions { get; private set; }
    public string? Description { get; private set; }

    private readonly List<MeetingAttendee> _attendees = [];
    public IReadOnlyList<MeetingAttendee> Attendees => _attendees;

    private Meeting()
    {
    }

    public Meeting(string title, MeetingType type, DateTimeOffset start, int time, string location)
    {
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));
        ArgumentException.ThrowIfNullOrEmpty(location, nameof(location));
        Title = title;
        Type = type;
        StartDateTime = start;
        Time = time;
        Location = location;
    }

    public void SetDecisions(string decisions)
    {
        ArgumentException.ThrowIfNullOrEmpty(decisions, nameof(decisions));
        Decisions = decisions;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();

    public void AddAttendee(MeetingAttendee attendee) => _attendees.Add(attendee);
}
