namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// How urgent a purchase request is, which decides where it sits in a buyer's queue.
/// </summary>
public enum RequisitionPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
