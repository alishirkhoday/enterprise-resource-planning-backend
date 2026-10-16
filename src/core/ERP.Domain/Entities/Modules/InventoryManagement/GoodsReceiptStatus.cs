namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a delivery has got to. Only a confirmed receipt has put anything on a shelf.
/// </summary>
public enum GoodsReceiptStatus
{
    Draft = 0,
    Confirmed = 1,
    Cancelled = 2
}
