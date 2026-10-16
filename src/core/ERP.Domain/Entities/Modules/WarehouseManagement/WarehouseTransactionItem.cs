using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// One line of a warehouse document.
/// </summary>
public sealed class WarehouseTransactionItem : BaseEntity
{
    public WarehouseTransaction Transaction { get; private set; } = default!;
    public Guid TransactionId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>
    /// What one unit is worth in this movement. Without it the inventory ledger cannot be valued
    /// and the cost of goods sold cannot be derived.
    /// </summary>
    public Money UnitCost { get; private set; } = default!;

    public Money TotalCost => UnitCost.Multiply(Quantity);

    public Guid? ItemBatchId { get; private set; }
    public Guid? SerialNumberId { get; private set; }
    public Guid? SourceStorageLocationId { get; private set; }
    public Guid? DestinationStorageLocationId { get; private set; }
    public string? Description { get; private set; }

    private WarehouseTransactionItem() { }

    public WarehouseTransactionItem(WarehouseTransaction transaction, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId, Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(transaction, nameof(transaction));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (unitCost.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitCost), "A unit cost cannot be negative.");

        Transaction = transaction;
        TransactionId = transaction.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitCost = unitCost;
    }

    public void SetTracking(Guid? itemBatchId, Guid? serialNumberId)
    {
        if (itemBatchId is not null && Item.TrackingType != ItemTrackingType.Batch)
            throw new InvalidOperationException($"Item {Item.Name} is not batch tracked.");
        if (serialNumberId is not null && Item.TrackingType != ItemTrackingType.Serial)
            throw new InvalidOperationException($"Item {Item.Name} is not serial tracked.");
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
    }

    public void SetStorageLocations(Guid? sourceStorageLocationId, Guid? destinationStorageLocationId)
    {
        SourceStorageLocationId = sourceStorageLocationId == Guid.Empty ? null : sourceStorageLocationId;
        DestinationStorageLocationId = destinationStorageLocationId == Guid.Empty ? null : destinationStorageLocationId;
    }

    public void SetUnitCost(Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        if (unitCost.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitCost), "A unit cost cannot be negative.");
        UnitCost = unitCost;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Whether the tracking identifiers this item demands have been supplied.</summary>
    public bool IsTrackingComplete => Item.TrackingType switch
    {
        ItemTrackingType.Batch => ItemBatchId is not null,
        ItemTrackingType.Serial => SerialNumberId is not null,
        _ => true
    };
}
