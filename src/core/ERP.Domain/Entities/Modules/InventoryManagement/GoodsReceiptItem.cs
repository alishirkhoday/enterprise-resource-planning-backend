namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// What actually arrived on one order line, split between what was accepted and what was turned away.
/// </summary>
public sealed class GoodsReceiptItem : BaseEntity
{
    public GoodsReceipt Receipt { get; private set; } = default!;
    public Guid ReceiptId { get; private set; }

    public PurchaseOrderItem OrderItem { get; private set; } = default!;
    public Guid OrderItemId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public decimal ReceivedQuantity { get; private set; }
    public decimal AcceptedQuantity { get; private set; }
    public decimal RejectedQuantity => ReceivedQuantity - AcceptedQuantity;

    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>The cost this arrival carries into inventory, including its share of freight.</summary>
    public Money UnitCost { get; private set; } = default!;
    public Money TotalCost => UnitCost.Multiply(AcceptedQuantity);

    public Guid? ItemBatchId { get; private set; }
    public Guid? StorageLocationId { get; private set; }
    public string? RejectionReason { get; private set; }
    public bool RequiresInspection { get; private set; }

    private readonly List<Guid> _serialNumberIds = [];
    public IReadOnlyList<Guid> SerialNumberIds => _serialNumberIds.AsReadOnly();

    private GoodsReceiptItem() { }

    public GoodsReceiptItem(GoodsReceipt receipt, PurchaseOrderItem orderItem, decimal receivedQuantity, Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(receipt, nameof(receipt));
        ArgumentNullException.ThrowIfNull(orderItem, nameof(orderItem));
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(receivedQuantity, nameof(receivedQuantity));
        if (receivedQuantity > orderItem.RemainingQuantity)
            throw new InvalidOperationException($"Only {orderItem.RemainingQuantity} is still outstanding on that order line.");

        Receipt = receipt;
        ReceiptId = receipt.Id;
        OrderItem = orderItem;
        OrderItemId = orderItem.Id;
        Item = orderItem.Item;
        ItemId = orderItem.ItemId;
        ReceivedQuantity = receivedQuantity;
        AcceptedQuantity = receivedQuantity;
        UnitOfMeasureId = orderItem.UnitOfMeasureId;
        UnitCost = unitCost;
    }

    /// <summary>Records that part of the arrival failed inspection.</summary>
    internal void Reject(decimal rejectedQuantity, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rejectedQuantity, nameof(rejectedQuantity));
        if (rejectedQuantity > ReceivedQuantity)
            throw new InvalidOperationException("Cannot reject more than arrived.");
        AcceptedQuantity = ReceivedQuantity - rejectedQuantity;
        RejectionReason = reason.Trim();
    }

    internal void SetBatch(Guid itemBatchId)
    {
        if (Item.TrackingType != ItemTrackingType.Batch)
            throw new InvalidOperationException($"Item {Item.Name} is not batch tracked.");
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
    }

    internal void AddSerialNumber(Guid serialNumberId)
    {
        if (Item.TrackingType != ItemTrackingType.Serial)
            throw new InvalidOperationException($"Item {Item.Name} is not serial tracked.");
        if (serialNumberId == Guid.Empty)
            throw new ArgumentException("SerialNumberId is empty.", nameof(serialNumberId));
        if (_serialNumberIds.Count >= AcceptedQuantity)
            throw new InvalidOperationException("There are already as many serials as accepted units.");
        _serialNumberIds.Add(serialNumberId);
    }

    internal void ScopeToStorageLocation(Guid storageLocationId) =>
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;

    internal void MarkAsRequiringInspection() => RequiresInspection = true;

    /// <summary>Whether the tracking identifiers this item demands have all been supplied.</summary>
    public bool IsTrackingComplete => Item.TrackingType switch
    {
        ItemTrackingType.Batch => ItemBatchId is not null,
        ItemTrackingType.Serial => _serialNumberIds.Count == AcceptedQuantity,
        _ => true
    };
}
