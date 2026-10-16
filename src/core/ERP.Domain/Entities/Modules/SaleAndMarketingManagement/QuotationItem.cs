using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// One line of a quotation: what is offered, at what price, with what discount and tax.
/// </summary>
public sealed class QuotationItem : BaseEntity
{
    public Quotation Quotation { get; private set; } = default!;
    public Guid QuotationId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitPrice { get; private set; } = default!;

    public Percentage DiscountPercentage { get; private set; } = default!;
    public Guid? TaxId { get; private set; }
    public Percentage TaxPercentage { get; private set; } = default!;

    public int? DeliveryLeadTimeDays { get; private set; }
    public string? Description { get; private set; }

    public Money GrossAmount => UnitPrice.Multiply(Quantity);
    public Money DiscountAmount => GrossAmount.Percent(DiscountPercentage);
    public Money LineTotal => GrossAmount.Subtract(DiscountAmount);
    public Money TaxAmount => LineTotal.Percent(TaxPercentage);
    public Money LineTotalWithTax => LineTotal.Add(TaxAmount);

    private QuotationItem() { }

    public QuotationItem(Quotation quotation, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId, Money unitPrice)
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

    public void SetDeliveryLeadTime(int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days, nameof(days));
        DeliveryLeadTimeDays = days;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
