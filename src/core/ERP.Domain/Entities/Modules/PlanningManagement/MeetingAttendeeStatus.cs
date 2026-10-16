namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// Whether an invitee accepted and whether they attended - which are not the same thing.
/// </summary>
public enum MeetingAttendeeStatus
{
    Invited = 0,
    Accepted = 1,
    Declined = 2,
    Tentative = 3
}
