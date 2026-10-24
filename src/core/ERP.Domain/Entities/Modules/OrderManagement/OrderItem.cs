using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// One line of an order: what was bought, how many, and at what price.
/// 
/// The price is copied onto the line rather than read from the item, because what the customer
/// agreed to pay must not change when somebody edits a price list afterwards.
/// </summary>
public sealed class OrderItem : BaseEntity
{
    public Order Order { get; private set; } = default!;
    public Guid OrderId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }

    /// <summary>Held as a decimal so fractional quantities stay exact, unlike a floating point count.</summary>
    public decimal Quantity { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }
    public Money ItemUnitPrice { get; private set; } = default!;
    public Money Discount { get; private set; } = default!;

    public Guid? TaxId { get; private set; }
    public Percentage TaxPercentage { get; private set; } = default!;

    public Guid? ItemBatchId { get; private set; }
    public string? Description { get; private set; }

    public Money Amount => ItemUnitPrice.Multiply(Quantity);
    public Money AmountPayable => Amount.Subtract(Discount);
    public Money TaxAmount => AmountPayable.Percent(TaxPercentage);
    public Money AmountPayableWithTax => AmountPayable.Add(TaxAmount);

    private OrderItem() { }

    public OrderItem(Order order, Item item, short lineNumber, Money itemUnitPrice, decimal quantity, Guid unitOfMeasureId)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(itemUnitPrice, nameof(itemUnitPrice));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (itemUnitPrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(itemUnitPrice), "A unit price cannot be negative.");
        if (!item.IsSellable)
            throw new InvalidOperationException($"Item {item.Name} is not marked as sellable.");

        Order = order;
        OrderId = order.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        ItemUnitPrice = itemUnitPrice;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        Discount = Money.Zero(itemUnitPrice.CurrencyCode);
        TaxPercentage = Percentage.Zero();
    }

    internal void SetDiscount(Money discount)
    {
        ArgumentNullException.ThrowIfNull(discount, nameof(discount));
        if (discount.CurrencyCode != ItemUnitPrice.CurrencyCode)
            throw new ArgumentException("The discount must share the line currency.", nameof(discount));
        if (discount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(discount), "A discount cannot be negative.");
        if (discount.Amount > Amount.Amount)
            throw new ArgumentOutOfRangeException(nameof(discount), "A discount cannot exceed the line amount.");
        Discount = discount;
    }

    internal void SetTax(Guid taxId, Percentage taxPercentage)
    {
        ArgumentNullException.ThrowIfNull(taxPercentage, nameof(taxPercentage));
        TaxId = taxId == Guid.Empty ? null : taxId;
        TaxPercentage = taxPercentage;
    }

    internal void SetBatch(Guid itemBatchId) => ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
