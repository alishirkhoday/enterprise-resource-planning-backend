namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Where one operation of a works order has got to. Every task has to be finished or
/// cancelled before the order can close.
/// </summary>
public enum ProductionTaskStatus
{
    Pending = 0,
    InProgress = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4
}
