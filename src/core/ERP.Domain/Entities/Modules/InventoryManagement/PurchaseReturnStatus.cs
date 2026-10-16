namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a return to a supplier has got to - برگشت از خرید.
/// </summary>
public enum PurchaseReturnStatus
{
    Draft = 0,

    /// <summary>Goods have left the warehouse.</summary>
    Confirmed = 1,

    /// <summary>The supplier has issued a credit note against it.</summary>
    Credited = 2,

    Cancelled = 3
}
