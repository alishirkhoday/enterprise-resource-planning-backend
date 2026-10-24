namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One line of a supplier's invoice, matched against what was ordered and what arrived.
/// </summary>
public sealed class PurchaseInvoiceItem : BaseEntity
{
    public PurchaseInvoice Invoice { get; private set; } = default!;
    public Guid InvoiceId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public Guid? PurchaseOrderItemId { get; private set; }
    public short LineNumber { get; private set; }

    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitPrice { get; private set; } = default!;
    public Percentage DiscountPercentage { get; private set; } = default!;

    public Guid? TaxId { get; private set; }
    public Percentage TaxPercentage { get; private set; } = default!;

    public Guid? AccountId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public string? Description { get; private set; }

    public Money GrossAmount => UnitPrice.Multiply(Quantity);
    public Money DiscountAmount => GrossAmount.Percent(DiscountPercentage);
    public Money LineTotal => GrossAmount.Subtract(DiscountAmount);
    public Money TaxAmount => LineTotal.Percent(TaxPercentage);
    public Money LineTotalWithTax => LineTotal.Add(TaxAmount);

    private PurchaseInvoiceItem() { }

    public PurchaseInvoiceItem(PurchaseInvoice invoice, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId, Money unitPrice)
    {
        ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(unitPrice, nameof(unitPrice));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (unitPrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "A unit price cannot be negative.");

        Invoice = invoice;
        InvoiceId = invoice.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitPrice = unitPrice;
        DiscountPercentage = Percentage.Zero();
        TaxPercentage = Percentage.Zero();
    }

    internal void LinkToOrderItem(Guid purchaseOrderItemId) =>
        PurchaseOrderItemId = purchaseOrderItemId == Guid.Empty ? null : purchaseOrderItemId;

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

    internal void SetPostingTargets(Guid? accountId, Guid? costCenterId)
    {
        AccountId = accountId == Guid.Empty ? null : accountId;
        CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
