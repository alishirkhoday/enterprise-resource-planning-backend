namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where an internal request to buy something has got to - درخواست خرید.
/// </summary>
public enum PurchaseRequisitionStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    PartiallyOrdered = 4,
    Ordered = 5,
    Cancelled = 6
}
