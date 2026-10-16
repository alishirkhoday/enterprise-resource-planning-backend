namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// What kind of trip a mission is - مأموریت. Decides what may be claimed for it.
/// </summary>
public enum MissionType
{
    /// <summary>Within the city the employee normally works in.</summary>
    Local = 0,

    /// <summary>Elsewhere in the country.</summary>
    Domestic = 1,

    International = 2,

    /// <summary>A training course held away from the workplace.</summary>
    Training = 3
}
