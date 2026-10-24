namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// A meeting - جلسه. Its minutes and decisions, so a commitment made in a room can be traced
/// later.
/// </summary>
public sealed partial class Meeting : AggregateRoot
{
    public string Title { get; private set; } = default!;
    public MeetingType Type { get; private set; } = default!;
    public DateTimeOffset StartDateTime { get; private set; }
    public int Time { get; private set; }
    public string Location { get; private set; } = default!;
    public string? Decisions { get; private set; }
    public string? Description { get; private set; }

    private readonly List<MeetingAttendee> _attendees = [];
    public IReadOnlyList<MeetingAttendee> Attendees => _attendees.AsReadOnly();

    private Meeting()
    {
    }

    public Meeting(string title, MeetingType type, DateTimeOffset start, int time, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(location, nameof(location));
        Title = title;
        Type = type;
        StartDateTime = start;
        Time = time;
        Location = location;
    }

    public void SetDecisions(string decisions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decisions, nameof(decisions));
        Decisions = decisions;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();

    public void AddAttendee(MeetingAttendee attendee)
    {
        ArgumentNullException.ThrowIfNull(attendee);
        EnsureCanAddChild(attendee, attendee.Meeting.Id, _attendees);

        _attendees.Add(attendee);
    }
}
