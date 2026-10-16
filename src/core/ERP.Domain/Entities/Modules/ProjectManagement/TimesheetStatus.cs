namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// Where booked time has got to. Only approved hours reach a project statement, so this is
/// what separates what somebody claims from what the project is charged.
/// </summary>
public enum TimesheetStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}
