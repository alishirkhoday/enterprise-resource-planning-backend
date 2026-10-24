namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// What one supplier charges for one item, and on what terms. Purchasing reads this to propose a
/// price and a delivery date without anyone retyping them.
/// </summary>
public sealed class ItemSupplier : BaseEntity
{
    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public Supplier Supplier { get; private set; } = default!;
    public Guid SupplierId { get; private set; }

    /// <summary>The code the supplier knows the item by, which their invoices will use.</summary>
    public string? SupplierItemCode { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }
    public Money LastPurchasePrice { get; private set; } = default!;
    public DateOnly? LastPurchaseDate { get; private set; }

    /// <summary>The smallest quantity the supplier will accept on an order.</summary>
    public decimal? MinimumOrderQuantity { get; private set; }

    /// <summary>The step the order quantity must be a multiple of.</summary>
    public decimal? OrderMultiple { get; private set; }

    public int? LeadTimeDays { get; private set; }
    public bool IsPreferred { get; private set; }
    public bool IsActive { get; private set; }
    public string? Description { get; private set; }

    private ItemSupplier() { }

    public ItemSupplier(Item item, Supplier supplier, Guid unitOfMeasureId, Money lastPurchasePrice)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        ArgumentNullException.ThrowIfNull(lastPurchasePrice, nameof(lastPurchasePrice));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (lastPurchasePrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(lastPurchasePrice), "A price cannot be negative.");

        Item = item;
        ItemId = item.Id;
        Supplier = supplier;
        SupplierId = supplier.Id;
        UnitOfMeasureId = unitOfMeasureId;
        LastPurchasePrice = lastPurchasePrice;
        IsActive = true;
    }

    internal void SetSupplierItemCode(string? supplierItemCode) =>
        SupplierItemCode = string.IsNullOrWhiteSpace(supplierItemCode) ? null : supplierItemCode.Trim();

    internal void RecordPurchase(Money price, DateOnly purchaseDate)
    {
        ArgumentNullException.ThrowIfNull(price, nameof(price));
        if (price.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(price), "A price cannot be negative.");
        LastPurchasePrice = price;
        LastPurchaseDate = purchaseDate;
    }

    internal void SetOrderingRules(decimal? minimumOrderQuantity, decimal? orderMultiple, int? leadTimeDays)
    {
        if (minimumOrderQuantity.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumOrderQuantity.Value, nameof(minimumOrderQuantity));
        if (orderMultiple.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderMultiple.Value, nameof(orderMultiple));
        if (leadTimeDays.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(leadTimeDays.Value, nameof(leadTimeDays));

        MinimumOrderQuantity = minimumOrderQuantity;
        OrderMultiple = orderMultiple;
        LeadTimeDays = leadTimeDays;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    internal void SetPreferred() => IsPreferred = true;
    internal void UnsetPreferred() => IsPreferred = false;

    internal void Activate() => IsActive = true;
    internal void Deactivate() => IsActive = false;

    /// <summary>Rounds a wanted quantity up to what the supplier will actually accept.</summary>
    public decimal NormalizeOrderQuantity(decimal wantedQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(wantedQuantity, nameof(wantedQuantity));
        var quantity = wantedQuantity;

        if (MinimumOrderQuantity.HasValue && quantity < MinimumOrderQuantity.Value)
            quantity = MinimumOrderQuantity.Value;

        if (OrderMultiple is > 0m)
            quantity = Math.Ceiling(quantity / OrderMultiple.Value) * OrderMultiple.Value;

        return quantity;
    }

    public DateOnly ExpectedDeliveryFrom(DateOnly orderDate) =>
        orderDate.AddDays(LeadTimeDays ?? Supplier.LeadTimeDays ?? 0);
}
