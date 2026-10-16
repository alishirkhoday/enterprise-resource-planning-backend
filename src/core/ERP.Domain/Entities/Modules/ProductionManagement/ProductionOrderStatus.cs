namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Where a works order has got to. Releasing is the line that matters: before it nothing can
/// be issued to the order, after it the order starts costing money.
/// </summary>
public enum ProductionOrderStatus
{
    Created = 0,

    /// <summary>Materials and capacity have been worked out.</summary>
    Planned = 1,

    /// <summary>Handed to the shop floor.</summary>
    Released = 2,

    InProgress = 3,
    OnHold = 4,
    Completed = 5,
    Cancelled = 6
}
