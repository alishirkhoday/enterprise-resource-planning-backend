using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A commitment to sell to a customer at agreed prices. Stock is reserved against it, and
/// deliveries and invoices are both matched back to it.
/// </summary>
public sealed partial class SaleOrder : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;

    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public DateTimeOffset OrderDateTime { get; private set; }
    public DateOnly? RequestedDeliveryDate { get; private set; }
    public SaleOrderStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public decimal ExchangeRate { get; private set; }

    public Guid? WarehouseId { get; private set; }
    public Guid? PriceListId { get; private set; }
    public Guid? PaymentTermId { get; private set; }
    public Guid? QuotationId { get; private set; }
    public Guid? SalesRepHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>Freight and handling charged to the customer on the order as a whole.</summary>
    public Money AdditionalCharges { get; private set; } = default!;

    public string? Description { get; private set; }
    public string? Note { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly List<SaleOrderItem> _items = [];
    public IReadOnlyList<SaleOrderItem> Items => _items.AsReadOnly();

    private SaleOrder() { }

    public SaleOrder(string number, Customer customer, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Number = number.Trim();
        Customer = customer;
        CustomerId = customer.Id;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ExchangeRate = 1m;
        OrderDateTime = DateTimeOffset.UtcNow;
        Status = SaleOrderStatus.Draft;
        AdditionalCharges = Money.Zero(CurrencyCode);
    }

    public void SetRequestedDeliveryDate(DateOnly requestedDeliveryDate) => RequestedDeliveryDate = requestedDeliveryDate;

    public void SetExchangeRate(decimal exchangeRate)
    {
        EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exchangeRate, nameof(exchangeRate));
        ExchangeRate = exchangeRate;
    }

    public void SetFulfillmentSource(Guid warehouseId) => WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;

    public void SetCommercialTerms(Guid? priceListId, Guid? paymentTermId, Guid? salesRepHumanId)
    {
        PriceListId = priceListId == Guid.Empty ? null : priceListId;
        PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
        SalesRepHumanId = salesRepHumanId == Guid.Empty ? null : salesRepHumanId;
    }

    public void SetSource(Guid quotationId) => QuotationId = quotationId == Guid.Empty ? null : quotationId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetAdditionalCharges(Money additionalCharges)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(additionalCharges, nameof(additionalCharges));
        if (additionalCharges.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"Additional charges must be expressed in {CurrencyCode}.", nameof(additionalCharges));
        AdditionalCharges = additionalCharges;
    }

    public void AddItem(SaleOrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.SaleOrderId, _items);

        EnsureDraft();
        if (item.UnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Order lines must be priced in {CurrencyCode}.");
        if (!item.Item.IsSellable)
            throw new InvalidOperationException($"Item {item.Item.Name} is not marked as sellable.");
        _items.Add(item);
    }

    public void RemoveItem(SaleOrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.SaleOrderId != Id || !_items.Contains(item))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Remove(item);
    }

    public Money SubTotal() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.LineTotal).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money TotalDiscount() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.DiscountAmount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money TotalTax() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.TaxAmount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money GrandTotal() => SubTotal().Add(TotalTax()).Add(AdditionalCharges);

    public void Submit()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A sale order cannot be submitted without lines.");
        Status = SaleOrderStatus.Submitted;
    }

    public void Confirm()
    {
        if (Status != SaleOrderStatus.Submitted)
            throw new InvalidOperationException($"Only a submitted order can be confirmed; this one is {Status}.");
        Status = SaleOrderStatus.Confirmed;
    }

    /// <summary>Recomputes the delivery state from what the lines have shipped.</summary>
    public void RefreshDeliveryStatus()
    {
        if (Status is SaleOrderStatus.Draft or SaleOrderStatus.Submitted or SaleOrderStatus.Cancelled)
            return;

        if (_items.All(i => i.IsFullyDelivered))
            Status = SaleOrderStatus.Delivered;
        else if (_items.Any(i => i.DeliveredQuantity > 0m))
            Status = SaleOrderStatus.PartiallyDelivered;
        else
            Status = SaleOrderStatus.Confirmed;
    }

    public void CloseShort(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status is SaleOrderStatus.Draft or SaleOrderStatus.Cancelled)
            throw new InvalidOperationException($"An order in state {Status} cannot be closed short.");
        Status = SaleOrderStatus.Closed;
        Note = reason.Trim();
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (_items.Any(i => i.DeliveredQuantity > 0m))
            throw new InvalidOperationException("An order with deliveries cannot be cancelled; close it short instead.");
        Status = SaleOrderStatus.Cancelled;
        CancellationReason = reason.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();

    public void SetNote(string? note) => Note = string.IsNullOrEmpty(note) ? null : note.Trim();

    public bool CanDeliver => Status is SaleOrderStatus.Confirmed or SaleOrderStatus.PartiallyDelivered;

    private void EnsureDraft()
    {
        if (Status != SaleOrderStatus.Draft)
            throw new InvalidOperationException($"The order is {Status} and can no longer be edited.");
    }
}
