namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Why goods went back. Part of how a supplier is scored.
/// </summary>
public enum PurchaseReturnReason
{
    Damaged = 0,
    WrongItem = 1,
    QualityFailure = 2,
    Excess = 3,
    Expired = 4,
    LateDelivery = 5,
    Other = 99
}
