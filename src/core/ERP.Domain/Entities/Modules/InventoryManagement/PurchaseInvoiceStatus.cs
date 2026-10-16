namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a supplier's invoice has got to - فاکتور خرید.
/// </summary>
public enum PurchaseInvoiceStatus
{
    Draft = 0,

    /// <summary>Held back from payment while a discrepancy is resolved.</summary>
    Disputed = 1,

    Posted = 2,
    Paid = 3,
    Cancelled = 4
}
