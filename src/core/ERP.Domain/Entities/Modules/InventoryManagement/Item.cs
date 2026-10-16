namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Anything the business buys, sells, makes or consumes - کالا.
/// 
/// The flags decide which modules may touch it: only a sellable item can go on an invoice,
/// only a manufactured one can be the output of a works order. Tracking decides how closely
/// individual units are followed - not at all, by batch, or one serial number at a time.
/// </summary>
public sealed class Item : BaseEntity
{
    public ItemGroup Group { get; private set; } = default!;
    public Guid GroupId { get; private set; }

    public ItemType Type { get; private set; }
    public string? StockKeepingUnit { get; private set; }
    public string? Name { get; private set; }
    public string? BarCode { get; private set; }
    public string? QRCode { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>The unit quantities of this item are stored in. Every other unit converts to it.</summary>
    public Guid? BaseUnitOfMeasureId { get; private set; }

    /// <summary>How the cost of an issue is determined.</summary>
    public CostingMethod CostingMethod { get; private set; }

    /// <summary>Whether individual units are tracked by batch, by serial, or not at all.</summary>
    public ItemTrackingType TrackingType { get; private set; }

    /// <summary>Days from production before the item expires, for a perishable item.</summary>
    public int? ShelfLifeDays { get; private set; }

    public decimal? WeightInKilograms { get; private set; }
    public decimal? VolumeInCubicMeters { get; private set; }

    public bool IsPurchasable { get; private set; }
    public bool IsSellable { get; private set; }
    public bool IsManufactured { get; private set; }

    public Guid? DefaultWarehouseId { get; private set; }
    public Guid? InventoryAccountId { get; private set; }

    private readonly List<ItemSpecification> _specifications = [];
    public IReadOnlyList<ItemSpecification> Specifications => _specifications.AsReadOnly();

    private readonly List<ItemBarcode> _barcodes = [];
    public IReadOnlyList<ItemBarcode> Barcodes => _barcodes;

    private readonly List<ItemSupplier> _suppliers = [];
    public IReadOnlyList<ItemSupplier> Suppliers => _suppliers;

    private Item() { }

    public Item(ItemGroup group, ItemType type)
    {
        ArgumentNullException.ThrowIfNull(group, nameof(group));
        Group = group;
        GroupId = group.Id;
        Type = type;
        CostingMethod = CostingMethod.WeightedAverage;
        TrackingType = ItemTrackingType.None;
        IsPurchasable = true;
        IsSellable = true;
    }

    public void Update(string sku, string name, string? barCode = null, string? qrCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku, nameof(sku));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        StockKeepingUnit = sku.Trim();
        Name = name.Trim();
        BarCode = barCode?.Trim();
        QRCode = qrCode?.Trim();
    }

    public void SetBaseUnitOfMeasure(Guid unitOfMeasureId)
    {
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        BaseUnitOfMeasureId = unitOfMeasureId;
    }

    public void SetCostingMethod(CostingMethod costingMethod) => CostingMethod = costingMethod;

    public void SetTracking(ItemTrackingType trackingType, int? shelfLifeDays = null)
    {
        if (shelfLifeDays.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(shelfLifeDays.Value, nameof(shelfLifeDays));
        if (trackingType == ItemTrackingType.Serial && shelfLifeDays.HasValue)
            throw new ArgumentException("A serial tracked item expresses expiry per serial, not per item.", nameof(shelfLifeDays));
        TrackingType = trackingType;
        ShelfLifeDays = shelfLifeDays;
    }

    public void SetPhysicalProperties(decimal? weightInKilograms, decimal? volumeInCubicMeters)
    {
        if (weightInKilograms.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(weightInKilograms.Value, nameof(weightInKilograms));
        if (volumeInCubicMeters.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(volumeInCubicMeters.Value, nameof(volumeInCubicMeters));
        WeightInKilograms = weightInKilograms;
        VolumeInCubicMeters = volumeInCubicMeters;
    }

    public void SetUsage(bool isPurchasable, bool isSellable, bool isManufactured)
    {
        IsPurchasable = isPurchasable;
        IsSellable = isSellable;
        IsManufactured = isManufactured;
    }

    public void SetDefaultWarehouse(Guid warehouseId) => DefaultWarehouseId = warehouseId == Guid.Empty ? null : warehouseId;

    public void LinkInventoryAccount(Guid accountId) => InventoryAccountId = accountId == Guid.Empty ? null : accountId;

    public void AddBarcode(ItemBarcode barcode)
    {
        ArgumentNullException.ThrowIfNull(barcode, nameof(barcode));
        if (_barcodes.Any(b => b.Value == barcode.Value))
            throw new InvalidOperationException($"Barcode {barcode.Value} is already registered for this item.");
        _barcodes.Add(barcode);
    }

    public void AddSupplier(ItemSupplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        if (_suppliers.Any(s => s.SupplierId == supplier.SupplierId && s.UnitOfMeasureId == supplier.UnitOfMeasureId))
            throw new InvalidOperationException("This supplier is already registered for the item in that unit.");
        if (supplier.IsPreferred)
            _suppliers.ForEach(s => s.UnsetPreferred());
        _suppliers.Add(supplier);
    }

    public ItemSupplier? PreferredSupplier() => _suppliers.FirstOrDefault(s => s.IsPreferred && s.IsActive);

    /// <summary>Whether an issue of this item must name a batch or a serial.</summary>
    public bool RequiresTracking => TrackingType != ItemTrackingType.None;

    public void SetActive() => IsActive = true;
    public void SetDeactive() => IsActive = false;
    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
}
