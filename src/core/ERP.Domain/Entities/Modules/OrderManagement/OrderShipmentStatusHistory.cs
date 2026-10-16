namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Every step a delivery went through, kept rather than overwritten. What a customer is shown
/// when they ask where their parcel is.
/// </summary>
public sealed class OrderShipmentStatusHistory : BaseEntity
{
    public Order Order { get; private set; } = default!;
    public Guid OrderId { get; private set; }

    public OrderShipmentStatus Status { get; private set; }
    public DateTimeOffset ChangedDateTime { get; private set; }
    public string? Description { get; private set; }
    public Guid? ChangedByUserId { get; private set; }

    private OrderShipmentStatusHistory() { }

    internal OrderShipmentStatusHistory(Order order, OrderShipmentStatus status)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        Order = order;
        OrderId = order.Id;
        Status = status;
        ChangedDateTime = DateTimeOffset.UtcNow;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void SetChangedBy(Guid userId) => ChangedByUserId = userId == Guid.Empty ? null : userId;
}
