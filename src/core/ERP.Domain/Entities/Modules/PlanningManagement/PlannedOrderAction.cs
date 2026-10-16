namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// What planning proposes for a line. Rescheduling an order that already exists is kept apart from
/// placing a new one, because moving a date costs a phone call and ordering again costs money.
/// </summary>
public enum PlannedOrderAction
{
    /// <summary>Enough is already on hand or on its way.</summary>
    None = 0,

    /// <summary>Buy it: the line should become a purchase requisition.</summary>
    Purchase = 1,

    /// <summary>Make it: the line should become a production order.</summary>
    Manufacture = 2,

    /// <summary>Move it: the quantity exists, but in another warehouse.</summary>
    Transfer = 3,

    /// <summary>Supply already on order arrives too late and should be pulled in.</summary>
    ExpediteExisting = 4,

    /// <summary>Supply arrives earlier than needed and is tying up money.</summary>
    DeferExisting = 5,

    /// <summary>Demand disappeared after the order was placed.</summary>
    CancelExisting = 6
}
