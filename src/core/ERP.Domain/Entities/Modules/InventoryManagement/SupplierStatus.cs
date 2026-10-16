namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Whether the business is still willing to buy from them.
/// </summary>
public enum SupplierStatus
{
    Active = 0,

    /// <summary>Temporarily not ordered from, pending a resolution.</summary>
    Suspended = 1,

    /// <summary>Permanently barred.</summary>
    Blacklisted = 2,

    /// <summary>Registered but not yet approved for ordering.</summary>
    Pending = 3
}
