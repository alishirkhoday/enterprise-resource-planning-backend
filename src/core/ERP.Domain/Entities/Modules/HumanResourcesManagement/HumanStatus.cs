namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Where somebody stands with the company. Only an active employee can be given a contract,
/// booked to a project or paid.
/// </summary>
public enum HumanStatus
{
    Active = 0,
    Suspended = 1,
    Terminated = 2,
    Retired = 3
}
