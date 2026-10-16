namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// How urgent a works order is, which decides where it sits in the shop floor's queue.
/// </summary>
public enum ProductionOrderPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
