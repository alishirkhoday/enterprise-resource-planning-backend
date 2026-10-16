namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// What state a returned item arrived in, which decides whether it goes back on the shelf,
/// into quarantine or straight to scrap.
/// </summary>
public enum ReturnItemCondition
{
    /// <summary>Sealed and resellable.</summary>
    AsNew = 0,

    /// <summary>Opened but undamaged, and can go back on the shelf.</summary>
    Opened = 1,

    Damaged = 2,
    Defective = 3,

    /// <summary>Fit only to be written off.</summary>
    Unusable = 4
}
