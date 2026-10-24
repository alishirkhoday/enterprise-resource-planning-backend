using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// One line the customer wants to return, and what state it came back in.
/// </summary>
public sealed class ReturnRequestItem : BaseEntity
{
    public ReturnRequest Request { get; private set; } = default!;
    public Guid RequestId { get; private set; }

    public OrderItem OrderItem { get; private set; } = default!;
    public Guid OrderItemId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public decimal RequestedQuantity { get; private set; }

    /// <summary>How much came back in a state fit to be put away.</summary>
    public decimal AcceptedQuantity { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitPrice { get; private set; } = default!;
    public Money LineTotal => UnitPrice.Multiply(AcceptedQuantity == 0m ? RequestedQuantity : AcceptedQuantity);

    public ReturnItemCondition? Condition { get; private set; }
    public Guid? SerialNumberId { get; private set; }
    public Guid? ItemBatchId { get; private set; }
    public string? Description { get; private set; }

    private ReturnRequestItem() { }

    public ReturnRequestItem(ReturnRequest request, OrderItem orderItem, decimal requestedQuantity)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(orderItem, nameof(orderItem));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requestedQuantity, nameof(requestedQuantity));
        if (requestedQuantity > orderItem.Quantity)
            throw new InvalidOperationException($"Only {orderItem.Quantity} was ordered on that line.");

        Request = request;
        RequestId = request.Id;
        OrderItem = orderItem;
        OrderItemId = orderItem.Id;
        Item = orderItem.Item;
        ItemId = orderItem.ItemId;
        RequestedQuantity = requestedQuantity;
        UnitOfMeasureId = orderItem.UnitOfMeasureId;
        UnitPrice = orderItem.ItemUnitPrice;
    }

    internal void Inspect(decimal acceptedQuantity, ReturnItemCondition condition, string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(acceptedQuantity, nameof(acceptedQuantity));
        if (acceptedQuantity > RequestedQuantity)
            throw new InvalidOperationException("More cannot be accepted than was requested.");
        AcceptedQuantity = acceptedQuantity;
        Condition = condition;
        Description = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    internal void SetTracking(Guid? itemBatchId, Guid? serialNumberId)
    {
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
    }

    /// <summary>Whether the returned units can go back into sellable stock.</summary>
    public bool IsRestockable => Condition is ReturnItemCondition.AsNew or ReturnItemCondition.Opened;
}
