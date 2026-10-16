namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Why the customer is sending it back.
/// </summary>
public enum ReturnReason
{
    Damaged = 0,
    WrongItem = 1,
    NotAsDescribed = 2,
    ChangedMind = 3,
    Defective = 4,
    LateDelivery = 5,
    SizeOrFit = 6,
    Other = 99
}
