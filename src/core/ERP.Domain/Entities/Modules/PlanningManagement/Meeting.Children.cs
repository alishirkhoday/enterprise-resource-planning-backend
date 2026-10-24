namespace ERP.Domain.Entities.Modules.PlanningManagement;

public sealed partial class Meeting
{
    public void UpdateMeetingAttendeeStatus(Guid childId, MeetingAttendeeStatus newStatus)
    {
        var child = GetChild(childId, _attendees);
        child.UpdateStatus(newStatus);
    }
}
