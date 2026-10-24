namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One line of a sales order: what was promised, and how much of it has gone out.
/// </summary>
public sealed class SaleOrderItem : BaseEntity
{
    public SaleOrder SaleOrder { get; private set; } = default!;
    public Guid SaleOrderId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitPrice { get; private set; } = default!;

    public Percentage DiscountPercentage { get; private set; } = default!;
    public Guid? TaxId { get; private set; }
    public Percentage TaxPercentage { get; private set; } = default!;

    /// <summary>Stock held for this line so it cannot be sold twice.</summary>
    public decimal ReservedQuantity { get; private set; }

    public decimal DeliveredQuantity { get; private set; }
    public decimal InvoicedQuantity { get; private set; }

    public Guid? WarehouseId { get; private set; }
    public DateOnly? PromisedDeliveryDate { get; private set; }
    public string? Description { get; private set; }

    public Money GrossAmount => UnitPrice.Multiply(Quantity);
    public Money DiscountAmount => GrossAmount.Percent(DiscountPercentage);
    public Money LineTotal => GrossAmount.Subtract(DiscountAmount);
    public Money TaxAmount => LineTotal.Percent(TaxPercentage);
    public Money LineTotalWithTax => LineTotal.Add(TaxAmount);

    public decimal RemainingQuantity => Quantity - DeliveredQuantity;
    public bool IsFullyDelivered => DeliveredQuantity >= Quantity;
    public bool IsFullyInvoiced => InvoicedQuantity >= Quantity;

    private SaleOrderItem() { }

    public SaleOrderItem(SaleOrder saleOrder, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId, Money unitPrice)
    {
        ArgumentNullException.ThrowIfNull(saleOrder, nameof(saleOrder));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(unitPrice, nameof(unitPrice));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (unitPrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "A unit price cannot be negative.");

        SaleOrder = saleOrder;
        SaleOrderId = saleOrder.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitPrice = unitPrice;
        DiscountPercentage = Percentage.Zero();
        TaxPercentage = Percentage.Zero();
    }

    internal void SetDiscount(Percentage discountPercentage)
    {
        ArgumentNullException.ThrowIfNull(discountPercentage, nameof(discountPercentage));
        DiscountPercentage = discountPercentage;
    }

    internal void SetTax(Guid taxId, Percentage taxPercentage)
    {
        ArgumentNullException.ThrowIfNull(taxPercentage, nameof(taxPercentage));
        TaxId = taxId == Guid.Empty ? null : taxId;
        TaxPercentage = taxPercentage;
    }

    internal void SetFulfillmentSource(Guid warehouseId) => WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;

    internal void SetPromisedDeliveryDate(DateOnly promisedDeliveryDate) => PromisedDeliveryDate = promisedDeliveryDate;

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    internal void ChangeQuantity(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity < DeliveredQuantity)
            throw new InvalidOperationException($"The quantity cannot drop below the {DeliveredQuantity} already delivered.");
        Quantity = quantity;
    }

    internal void Reserve(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (ReservedQuantity + quantity > Quantity)
            throw new InvalidOperationException("Reserving this quantity would exceed what was ordered.");
        ReservedQuantity += quantity;
    }

    internal void ReleaseReservation(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > ReservedQuantity)
            throw new InvalidOperationException("Cannot release more than is reserved.");
        ReservedQuantity -= quantity;
    }

    internal void Deliver(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (DeliveredQuantity + quantity > Quantity)
            throw new InvalidOperationException($"Delivering {quantity} would exceed the ordered {Quantity}; only {RemainingQuantity} is outstanding.");
        DeliveredQuantity += quantity;
        if (ReservedQuantity > 0m)
            ReservedQuantity -= Math.Min(quantity, ReservedQuantity);
    }

    internal void ReverseDelivery(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > DeliveredQuantity)
            throw new InvalidOperationException("Cannot reverse more than has been delivered.");
        DeliveredQuantity -= quantity;
    }

    internal void Invoice(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (InvoicedQuantity + quantity > Quantity)
            throw new InvalidOperationException("Invoicing this quantity would exceed what was ordered.");
        InvoicedQuantity += quantity;
    }
}
