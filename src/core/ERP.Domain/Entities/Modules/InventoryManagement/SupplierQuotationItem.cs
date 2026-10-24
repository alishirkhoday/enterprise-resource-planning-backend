namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One line a supplier priced, which is what a comparison is built from.
/// </summary>
public sealed class SupplierQuotationItem : BaseEntity
{
    public SupplierQuotation Quotation { get; private set; } = default!;
    public Guid QuotationId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitPrice { get; private set; } = default!;
    public Percentage DiscountPercentage { get; private set; } = default!;

    public int? LeadTimeDays { get; private set; }
    public string? SupplierItemCode { get; private set; }
    public string? Description { get; private set; }

    public Money GrossAmount => UnitPrice.Multiply(Quantity);
    public Money DiscountAmount => GrossAmount.Percent(DiscountPercentage);
    public Money LineTotal => GrossAmount.Subtract(DiscountAmount);
    public Money EffectiveUnitPrice => Quantity == 0m ? UnitPrice : LineTotal.Divide(Quantity);

    private SupplierQuotationItem() { }

    public SupplierQuotationItem(SupplierQuotation quotation, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId, Money unitPrice)
    {
        ArgumentNullException.ThrowIfNull(quotation, nameof(quotation));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(unitPrice, nameof(unitPrice));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (unitPrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "A unit price cannot be negative.");

        Quotation = quotation;
        QuotationId = quotation.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitPrice = unitPrice;
        DiscountPercentage = Percentage.Zero();
    }

    internal void SetDiscount(Percentage discountPercentage)
    {
        ArgumentNullException.ThrowIfNull(discountPercentage, nameof(discountPercentage));
        DiscountPercentage = discountPercentage;
    }

    internal void SetLeadTime(int leadTimeDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(leadTimeDays, nameof(leadTimeDays));
        LeadTimeDays = leadTimeDays;
    }

    internal void SetSupplierItemCode(string? supplierItemCode) =>
        SupplierItemCode = string.IsNullOrWhiteSpace(supplierItemCode) ? null : supplierItemCode.Trim();

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
