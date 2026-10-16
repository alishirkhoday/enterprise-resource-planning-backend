namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a purchase order has got to. Only an approved or partly received order can take a
/// delivery.
/// </summary>
public enum PurchaseOrderStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    PartiallyReceived = 3,
    Received = 4,

    /// <summary>Ended before full delivery, accepting what arrived.</summary>
    Closed = 5,

    Cancelled = 6
}
