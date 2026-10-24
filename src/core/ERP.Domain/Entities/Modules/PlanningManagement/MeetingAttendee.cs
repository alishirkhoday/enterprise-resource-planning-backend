namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// Somebody invited to a meeting, and whether they came.
/// </summary>
public sealed class MeetingAttendee : BaseEntity
{
    public Meeting Meeting { get; private set; } = default!;
    public string Prefix { get; private set; } = default!; //آقا یا خانم
    public string FullName { get; private set; } = default!;
    public string MobilePhoneNumberWithRegionCode { get; private set; } = default!;
    public string? Email { get; private set; }
    public MeetingAttendeeStatus Status { get; private set; }

    private MeetingAttendee()
    {
    }

    public MeetingAttendee(Meeting meeting, string prefix, string fullName, string mobilePhoneNumberWithRegionCode, string? email = null)
    {
        ArgumentNullException.ThrowIfNull(meeting, nameof(meeting));
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix, nameof(prefix));
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        ArgumentException.ThrowIfNullOrWhiteSpace(mobilePhoneNumberWithRegionCode, nameof(mobilePhoneNumberWithRegionCode));
        Meeting = meeting;
        Prefix = prefix;
        FullName = fullName;
        Email = email;
        Status = MeetingAttendeeStatus.Invited;
    }

    internal void UpdateStatus(MeetingAttendeeStatus newStatus) => Status = newStatus;
}
