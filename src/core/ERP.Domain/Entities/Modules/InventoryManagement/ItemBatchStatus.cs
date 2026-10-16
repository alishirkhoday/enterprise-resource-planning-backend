namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Whether a batch may still be used. Quarantined and expired stock is on the shelf but not
/// available, which is why this is not just a date comparison.
/// </summary>
public enum ItemBatchStatus
{
    Available = 0,
    Quarantined = 1,
    Depleted = 2,
    Expired = 3,
    Recalled = 4
}
