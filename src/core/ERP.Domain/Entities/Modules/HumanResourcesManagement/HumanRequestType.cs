namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// What kind of thing is being asked for.
/// </summary>
public enum HumanRequestType
{
    LeaveDaily = 0,
    LeaveHourly = 1,
    RemoteWork = 2,
    MissionWork = 3,
    ForgetAttendanceRecord = 4,
}
