namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Where a maintenance job has got to.
/// </summary>
public enum MaintenanceWorkOrderStatus
{
    Requested = 0,
    Approved = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
