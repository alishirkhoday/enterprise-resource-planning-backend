namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// How closely individual units are followed - سریال یا سری ساخت. Decides whether a movement
/// has to name a batch or a serial number, and therefore what a recall can trace.
/// </summary>
public enum ItemTrackingType
{
    /// <summary>Only quantities are tracked.</summary>
    None = 0,

    /// <summary>Units are grouped into batches that share a production and expiry date.</summary>
    Batch = 1,

    /// <summary>Every single unit carries its own identifier.</summary>
    Serial = 2
}
