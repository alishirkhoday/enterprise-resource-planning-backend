namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One individually identified unit of an item, followed from receipt through to the customer
/// who ended up with it.
/// </summary>
public sealed class SerialNumber : BaseEntity
{
    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public string Value { get; private set; } = default!;
    public SerialNumberStatus Status { get; private set; }

    public Guid? ItemBatchId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? StorageLocationId { get; private set; }

    public DateOnly? ProductionDate { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }
    public DateOnly? ReceivedOn { get; private set; }
    public DateOnly? SoldOn { get; private set; }

    /// <summary>The subsidiary account of whoever the unit was sold to.</summary>
    public Guid? SoldToDetailAccountId { get; private set; }

    /// <summary>The cost this specific unit carries, for specific identification costing.</summary>
    public Money? UnitCost { get; private set; }

    public DateOnly? WarrantyExpiresOn { get; private set; }
    public string? Description { get; private set; }

    private SerialNumber() { }

    public SerialNumber(Item item, string value)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        if (item.TrackingType != ItemTrackingType.Serial)
            throw new InvalidOperationException($"Item {item.Name} is not serial tracked.");

        Item = item;
        ItemId = item.Id;
        Value = value.Trim();
        Status = SerialNumberStatus.Expected;
    }

    public void SetBatch(Guid itemBatchId) => ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;

    public void SetDates(DateOnly? productionDate, DateOnly? expirationDate)
    {
        if (productionDate.HasValue && expirationDate.HasValue && expirationDate.Value < productionDate.Value)
            throw new ArgumentOutOfRangeException(nameof(expirationDate), "Expiry cannot precede production.");
        ProductionDate = productionDate;
        ExpirationDate = expirationDate;
    }

    public void SetUnitCost(Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        if (unitCost.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitCost), "A unit cost cannot be negative.");
        UnitCost = unitCost;
    }

    public void Receive(Guid warehouseId, DateOnly receivedOn, Guid? storageLocationId = null)
    {
        if (Status is not (SerialNumberStatus.Expected or SerialNumberStatus.Returned))
            throw new InvalidOperationException($"A serial in state {Status} cannot be received.");
        WarehouseId = warehouseId;
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;
        ReceivedOn = receivedOn;
        Status = SerialNumberStatus.InStock;
    }

    public void Reserve()
    {
        if (Status != SerialNumberStatus.InStock)
            throw new InvalidOperationException($"A serial in state {Status} cannot be reserved.");
        Status = SerialNumberStatus.Reserved;
    }

    public void ReleaseReservation()
    {
        if (Status != SerialNumberStatus.Reserved)
            throw new InvalidOperationException("Only a reserved serial can be released.");
        Status = SerialNumberStatus.InStock;
    }

    public void Sell(DateOnly soldOn, Guid? soldToDetailAccountId = null, int? warrantyMonths = null)
    {
        if (Status is not (SerialNumberStatus.InStock or SerialNumberStatus.Reserved))
            throw new InvalidOperationException($"A serial in state {Status} cannot be sold.");
        SoldOn = soldOn;
        SoldToDetailAccountId = soldToDetailAccountId == Guid.Empty ? null : soldToDetailAccountId;
        WarrantyExpiresOn = warrantyMonths.HasValue ? soldOn.AddMonths(warrantyMonths.Value) : null;
        Status = SerialNumberStatus.Sold;
    }

    public void Return(DateOnly returnedOn)
    {
        if (Status != SerialNumberStatus.Sold)
            throw new InvalidOperationException("Only a sold serial can be returned.");
        Status = SerialNumberStatus.Returned;
        SoldOn = null;
        SoldToDetailAccountId = null;
    }

    public void MoveTo(Guid warehouseId, Guid? storageLocationId = null)
    {
        if (Status is not (SerialNumberStatus.InStock or SerialNumberStatus.Quarantined))
            throw new InvalidOperationException($"A serial in state {Status} cannot be moved.");
        WarehouseId = warehouseId;
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;
    }

    public void Quarantine(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = SerialNumberStatus.Quarantined;
        Description = reason.Trim();
    }

    public void Scrap(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = SerialNumberStatus.Scrapped;
        Description = reason.Trim();
    }

    public bool IsExpiredOn(DateOnly date) => ExpirationDate.HasValue && ExpirationDate.Value < date;

    public bool IsUnderWarrantyOn(DateOnly date) => WarrantyExpiresOn.HasValue && WarrantyExpiresOn.Value >= date;
}
