namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// How urgent a maintenance job is.
/// </summary>
public enum MaintenancePriority
{
    Low = 0,
    Normal = 1,
    High = 2,

    /// <summary>The line is stopped until it is fixed.</summary>
    Emergency = 3
}
