namespace ERP.Domain.Events;

/// <summary>
/// Stock changed hands - every receipt, issue, transfer and adjustment raises this.
/// <para>
/// Both the change and the balance it left behind travel with it. A listener that only saw the
/// change would have to read the ledger to know where the stock now stands, which is the query
/// this event exists to save.
/// </para>
/// </summary>
/// <param name="ItemId">What moved.</param>
/// <param name="WarehouseId">Where it moved.</param>
/// <param name="QuantityChange">Positive coming in, negative going out.</param>
/// <param name="BalanceQuantity">What is on hand after the movement.</param>
/// <param name="MovementType">Why it moved, as the movement type's name.</param>
public sealed record StockMoved(
    Guid ItemId,
    Guid WarehouseId,
    decimal QuantityChange,
    decimal BalanceQuantity,
    string MovementType) : DomainEvent;

/// <summary>
/// Stock dropped under the minimum somebody set for it. A warning rather than an instruction:
/// what to do about it is a purchasing decision.
/// </summary>
/// <param name="ItemId">The item.</param>
/// <param name="WarehouseId">Where it ran low.</param>
/// <param name="OnHandQuantity">What is left.</param>
/// <param name="MinimumQuantity">The floor it fell under.</param>
public sealed record StockFellBelowMinimum(Guid ItemId, Guid WarehouseId, decimal OnHandQuantity, decimal MinimumQuantity) : DomainEvent;

/// <summary>
/// Stock reached the point where the policy says to order more. Kept apart from falling below
/// the minimum: a reorder point is a plan, a minimum is a problem.
/// </summary>
/// <param name="ItemId">The item.</param>
/// <param name="WarehouseId">Where.</param>
/// <param name="ReorderQuantity">How much the policy says to order.</param>
public sealed record ReorderPointReached(Guid ItemId, Guid WarehouseId, decimal ReorderQuantity) : DomainEvent;

/// <summary>A purchase order was approved and can now be sent to the supplier.</summary>
/// <param name="PurchaseOrderId">The order.</param>
/// <param name="Number">Its number.</param>
/// <param name="SupplierId">Who it goes to.</param>
public sealed record PurchaseOrderApproved(Guid PurchaseOrderId, string Number, Guid SupplierId) : DomainEvent;

/// <summary>A delivery was booked in against a purchase order.</summary>
/// <param name="GoodsReceiptId">The receipt.</param>
/// <param name="PurchaseOrderId">The order it satisfies.</param>
/// <param name="WarehouseId">Where the goods went.</param>
public sealed record GoodsReceived(Guid GoodsReceiptId, Guid PurchaseOrderId, Guid WarehouseId) : DomainEvent;

/// <summary>
/// A physical count disagreed with the books - مغایرت انبارگردانی. Raised per item rather than
/// per sheet, because each discrepancy is investigated on its own.
/// </summary>
/// <param name="StockCountId">The count sheet it was found on.</param>
/// <param name="ItemId">The item that did not match.</param>
/// <param name="Variance">Counted less what the books said: positive is a surplus.</param>
public sealed record StockCountDiscrepancyFound(Guid StockCountId, Guid ItemId, decimal Variance) : DomainEvent;
