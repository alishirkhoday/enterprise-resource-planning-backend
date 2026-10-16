namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// What a machine is doing. Only a running machine can have work scheduled on it.
/// </summary>
public enum MachineStatus
{
    Operational = 0,
    UnderMaintenance = 1,
    BrokenDown = 2,

    /// <summary>Working but held back from production, for instance while being set up.</summary>
    Idle = 3,

    Retired = 4
}
