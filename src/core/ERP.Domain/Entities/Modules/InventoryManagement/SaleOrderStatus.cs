namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a sales order has got to, as the fulfilment side sees it.
/// </summary>
public enum SaleOrderStatus
{
    Draft = 0,
    Submitted = 1,
    Confirmed = 2,
    PartiallyDelivered = 3,
    Delivered = 4,

    /// <summary>Ended before full delivery.</summary>
    Closed = 5,

    Cancelled = 6
}
