namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One item on a purchase order, at an agreed price, with what has actually been received
/// against it kept alongside.
/// </summary>
public sealed class PurchaseOrderItem : BaseEntity
{
    public PurchaseOrder PurchaseOrder { get; private set; } = default!;
    public Guid PurchaseOrderId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitPrice { get; private set; } = default!;

    public Percentage DiscountPercentage { get; private set; } = default!;
    public Guid? TaxId { get; private set; }
    public Percentage TaxPercentage { get; private set; } = default!;

    /// <summary>How much of the ordered quantity has arrived.</summary>
    public decimal ReceivedQuantity { get; private set; }

    /// <summary>How much of it has been invoiced by the supplier.</summary>
    public decimal InvoicedQuantity { get; private set; }

    public DateOnly? ExpectedDeliveryDate { get; private set; }
    public string? Description { get; private set; }

    public Money GrossAmount => UnitPrice.Multiply(Quantity);
    public Money DiscountAmount => GrossAmount.Percent(DiscountPercentage);
    public Money LineTotal => GrossAmount.Subtract(DiscountAmount);
    public Money TaxAmount => LineTotal.Percent(TaxPercentage);
    public Money LineTotalWithTax => LineTotal.Add(TaxAmount);

    public decimal RemainingQuantity => Quantity - ReceivedQuantity;
    public bool IsFullyReceived => ReceivedQuantity >= Quantity;
    public bool IsFullyInvoiced => InvoicedQuantity >= Quantity;

    private PurchaseOrderItem() { }

    public PurchaseOrderItem(PurchaseOrder purchaseOrder, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId, Money unitPrice)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder, nameof(purchaseOrder));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(unitPrice, nameof(unitPrice));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (unitPrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "A unit price cannot be negative.");

        PurchaseOrder = purchaseOrder;
        PurchaseOrderId = purchaseOrder.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitPrice = unitPrice;
        DiscountPercentage = Percentage.Zero();
        TaxPercentage = Percentage.Zero();
    }

    public void SetDiscount(Percentage discountPercentage)
    {
        ArgumentNullException.ThrowIfNull(discountPercentage, nameof(discountPercentage));
        DiscountPercentage = discountPercentage;
    }

    public void SetTax(Guid taxId, Percentage taxPercentage)
    {
        ArgumentNullException.ThrowIfNull(taxPercentage, nameof(taxPercentage));
        TaxId = taxId == Guid.Empty ? null : taxId;
        TaxPercentage = taxPercentage;
    }

    public void SetExpectedDeliveryDate(DateOnly expectedDeliveryDate) => ExpectedDeliveryDate = expectedDeliveryDate;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void ChangeQuantity(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity < ReceivedQuantity)
            throw new InvalidOperationException($"The quantity cannot drop below the {ReceivedQuantity} already received.");
        Quantity = quantity;
    }

    /// <summary>Records an arrival, refusing to take the line past what was ordered.</summary>
    public void Receive(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (ReceivedQuantity + quantity > Quantity)
            throw new InvalidOperationException($"Receiving {quantity} would exceed the ordered {Quantity}; only {RemainingQuantity} is outstanding.");
        ReceivedQuantity += quantity;
    }

    public void ReverseReceipt(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > ReceivedQuantity)
            throw new InvalidOperationException("Cannot reverse more than has been received.");
        ReceivedQuantity -= quantity;
    }

    public void Invoice(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (InvoicedQuantity + quantity > Quantity)
            throw new InvalidOperationException("Invoicing this quantity would exceed what was ordered.");
        InvoicedQuantity += quantity;
    }
}
