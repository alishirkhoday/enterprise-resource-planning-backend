namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A production run of an item that shares one set of dates and one cost. Batch tracking is what
/// makes a recall possible.
/// </summary>
public sealed class ItemBatch : BaseEntity
{
    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public string BatchNumber { get; private set; } = default!;
    public DateOnly ProductionDate { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }

    public decimal InitialQuantity { get; private set; }
    public decimal RemainingQuantity { get; private set; }

    public Guid? WarehouseId { get; private set; }
    public Guid? SupplierId { get; private set; }

    /// <summary>The cost one unit of this batch carries, used by first in first out costing.</summary>
    public Money? UnitCost { get; private set; }

    public ItemBatchStatus Status { get; private set; }
    public string? Description { get; private set; }

    private ItemBatch() { }

    public ItemBatch(Item item, string batchNumber, DateOnly productionDate, decimal initialQuantity)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentException.ThrowIfNullOrWhiteSpace(batchNumber, nameof(batchNumber));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialQuantity, nameof(initialQuantity));

        Item = item;
        ItemId = item.Id;
        BatchNumber = batchNumber.Trim();
        ProductionDate = productionDate;
        InitialQuantity = initialQuantity;
        RemainingQuantity = initialQuantity;
        Status = ItemBatchStatus.Available;

        if (item.ShelfLifeDays.HasValue)
            ExpirationDate = productionDate.AddDays(item.ShelfLifeDays.Value);
    }

    public void SetExpirationDate(DateOnly expirationDate)
    {
        if (expirationDate < ProductionDate)
            throw new ArgumentOutOfRangeException(nameof(expirationDate), "Expiry cannot precede production.");
        ExpirationDate = expirationDate;
    }

    public void SetUnitCost(Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        if (unitCost.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitCost), "A unit cost cannot be negative.");
        UnitCost = unitCost;
    }

    public void SetOrigin(Guid? warehouseId, Guid? supplierId)
    {
        WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;
        SupplierId = supplierId == Guid.Empty ? null : supplierId;
    }

    public void Consume(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (Status != ItemBatchStatus.Available)
            throw new InvalidOperationException($"Batch {BatchNumber} is {Status} and cannot be consumed.");
        if (quantity > RemainingQuantity)
            throw new InvalidOperationException($"Batch {BatchNumber} has only {RemainingQuantity} left.");

        RemainingQuantity -= quantity;
        if (RemainingQuantity == 0m)
            Status = ItemBatchStatus.Depleted;
    }

    public void Return(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (RemainingQuantity + quantity > InitialQuantity)
            throw new InvalidOperationException("A return cannot take the batch above what it started with.");
        RemainingQuantity += quantity;
        if (Status == ItemBatchStatus.Depleted)
            Status = ItemBatchStatus.Available;
    }

    public void Quarantine(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = ItemBatchStatus.Quarantined;
        Description = reason.Trim();
    }

    public void Release()
    {
        if (Status != ItemBatchStatus.Quarantined)
            throw new InvalidOperationException("Only a quarantined batch can be released.");
        Status = RemainingQuantity == 0m ? ItemBatchStatus.Depleted : ItemBatchStatus.Available;
    }

    public void MarkAsExpired() => Status = ItemBatchStatus.Expired;

    public void Recall(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = ItemBatchStatus.Recalled;
        Description = reason.Trim();
    }

    public bool IsExpiredOn(DateOnly date) => ExpirationDate.HasValue && ExpirationDate.Value < date;

    public int? DaysUntilExpiry(DateOnly from) => ExpirationDate.HasValue ? ExpirationDate.Value.DayNumber - from.DayNumber : null;

    public decimal ConsumedQuantity => InitialQuantity - RemainingQuantity;
}
