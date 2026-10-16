using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.PricingManagement;

/// <summary>
/// The price of one item on one list, optionally from a minimum quantity so volume breaks can
/// be expressed as several rows for the same item.
/// </summary>
public sealed class PriceListItem : BaseEntity
{
    public PriceList PriceList { get; private set; } = default!;
    public Guid PriceListId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }
    public Money BasePrice { get; private set; } = default!;

    /// <summary>The quantity this price starts applying from.</summary>
    public decimal MinimumQuantity { get; private set; }

    public Percentage? DiscountPercentage { get; private set; }
    public Guid? TaxId { get; private set; }
    public Percentage? TaxPercentage { get; private set; }

    /// <summary>The floor a salesperson may not discount below.</summary>
    public Money? MinimumPrice { get; private set; }

    public bool IsActive { get; private set; }

    public Money NetPrice => DiscountPercentage is null
        ? BasePrice
        : BasePrice.Subtract(BasePrice.Percent(DiscountPercentage));

    public Money TaxAmount => TaxPercentage is null
        ? Money.Zero(BasePrice.CurrencyCode)
        : NetPrice.Percent(TaxPercentage);

    public Money PriceWithTax => NetPrice.Add(TaxAmount);

    private PriceListItem() { }

    public PriceListItem(PriceList priceList, Item item, Guid unitOfMeasureId, Money basePrice, decimal minimumQuantity = 1m)
    {
        ArgumentNullException.ThrowIfNull(priceList, nameof(priceList));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(basePrice, nameof(basePrice));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumQuantity, nameof(minimumQuantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (basePrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(basePrice), "A price cannot be negative.");

        PriceList = priceList;
        PriceListId = priceList.Id;
        Item = item;
        ItemId = item.Id;
        UnitOfMeasureId = unitOfMeasureId;
        BasePrice = basePrice;
        MinimumQuantity = minimumQuantity;
        IsActive = true;
    }

    public void SetBasePrice(Money basePrice)
    {
        ArgumentNullException.ThrowIfNull(basePrice, nameof(basePrice));
        if (basePrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(basePrice), "A price cannot be negative.");
        BasePrice = basePrice;
    }

    public void SetDiscount(Percentage? discountPercentage) => DiscountPercentage = discountPercentage;

    public void SetTax(Guid? taxId, Percentage? taxPercentage)
    {
        TaxId = taxId == Guid.Empty ? null : taxId;
        TaxPercentage = taxPercentage;
    }

    public void SetMinimumPrice(Money minimumPrice)
    {
        ArgumentNullException.ThrowIfNull(minimumPrice, nameof(minimumPrice));
        if (minimumPrice.CurrencyCode != BasePrice.CurrencyCode)
            throw new ArgumentException("The floor must share the price currency.", nameof(minimumPrice));
        if (minimumPrice.Amount > BasePrice.Amount)
            throw new ArgumentOutOfRangeException(nameof(minimumPrice), "The floor cannot be above the base price.");
        MinimumPrice = minimumPrice;
    }

    /// <summary>Whether a proposed selling price respects the floor set on this line.</summary>
    public bool IsPriceAcceptable(Money proposedPrice)
    {
        ArgumentNullException.ThrowIfNull(proposedPrice, nameof(proposedPrice));
        return MinimumPrice is null || proposedPrice.Amount >= MinimumPrice.Amount;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
