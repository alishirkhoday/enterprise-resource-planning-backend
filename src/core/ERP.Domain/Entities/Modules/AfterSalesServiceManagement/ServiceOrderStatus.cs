namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// Where a service visit has got to.
/// </summary>
public enum ServiceOrderStatus
{
    Open = 0,
    Assigned = 1,
    Scheduled = 2,
    InProgress = 3,

    /// <summary>On hold until a part arrives.</summary>
    AwaitingParts = 4,

    Completed = 5,
    Cancelled = 6
}
