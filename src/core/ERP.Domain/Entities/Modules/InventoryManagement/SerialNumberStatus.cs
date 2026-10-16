namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where one tracked unit is in its life - in stock, sold, returned, scrapped.
/// </summary>
public enum SerialNumberStatus
{
    /// <summary>Registered on an order but not yet physically received.</summary>
    Expected = 0,

    InStock = 1,
    Reserved = 2,
    Sold = 3,
    Returned = 4,
    Quarantined = 5,
    Scrapped = 6
}
