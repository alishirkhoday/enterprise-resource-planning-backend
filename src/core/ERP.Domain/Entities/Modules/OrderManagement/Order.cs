using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;

namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// A retail order placed by a customer, paid for up front and shipped to an address captured at
/// the moment it was placed.
/// </summary>
public sealed partial class Order : AggregateRoot, IBranchScoped
{
    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public string Code { get; private set; } = default!;
    public OrderStatus Status { get; private set; }

    public DateTimeOffset RegistrationDateTime { get; private set; }
    public DateTimeOffset TimeLeftToPayment { get; private set; }

    public ShipToAddress ShipToAddress { get; private set; } = default!;
    public string CurrencyCode { get; private set; } = default!;

    public Money OrderSendCostAmount { get; private set; } = default!;

    public Guid? DiscountCodeId { get; private set; }
    public Money DiscountCodeAmount { get; private set; } = default!;

    public Guid? WarehouseId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private readonly List<Payment> _payments = [];
    public IReadOnlyList<Payment> Payments => _payments.AsReadOnly();

    private readonly List<OrderShipmentStatusHistory> _shipmentStatusHistories = [];
    public IReadOnlyList<OrderShipmentStatusHistory> ShipmentStatusHistories => _shipmentStatusHistories.AsReadOnly();

    public Money TotalAmount => Sum(_items.Select(i => i.Amount));
    public Money TotalDiscounts => Sum(_items.Select(i => i.Discount)).Add(DiscountCodeAmount);
    public Money TotalTax => Sum(_items.Select(i => i.TaxAmount));

    public Money AmountPayable =>
        Sum(_items.Select(i => i.AmountPayable))
            .Subtract(DiscountCodeAmount)
            .Add(TotalTax)
            .Add(OrderSendCostAmount);

    public Money PaidAmount =>
        Sum(_payments.Where(p => p.Status == PaymentStatus.Succeeded && p.AmountPaid is not null)
                     .Select(p => p.AmountPaid!));

    public Money OutstandingAmount => AmountPayable.Subtract(PaidAmount);
    public bool IsFullyPaid => !OutstandingAmount.IsPositive;

    private Order() { }

    public Order(Customer customer, string code, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (!customer.CanBeSoldTo)
            throw new InvalidOperationException($"Customer {customer.DisplayName} is {customer.Status}.");

        Customer = customer;
        CustomerId = customer.Id;
        Code = code.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        RegistrationDateTime = DateTimeOffset.UtcNow;
        Status = OrderStatus.Pending;
        OrderSendCostAmount = Money.Zero(CurrencyCode);
        DiscountCodeAmount = Money.Zero(CurrencyCode);
    }

    public void SetTimeLeftToPayment(int timeLeftToPayment)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeLeftToPayment, nameof(timeLeftToPayment));
        TimeLeftToPayment = RegistrationDateTime.AddMinutes(timeLeftToPayment);
    }

    public void SetItems(IEnumerable<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        EnsurePending();
        var replacement = new List<OrderItem>();
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            EnsureCanAddChild(item, item.OrderId, replacement);
            if (item.ItemUnitPrice.CurrencyCode != CurrencyCode)
                throw new InvalidOperationException($"Lines must be priced in {CurrencyCode}.");
            replacement.Add(item);
        }
        _items.Clear();
        _items.AddRange(replacement);
    }

    public void AddItem(OrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.OrderId, _items);

        EnsurePending();
        if (item.ItemUnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Order lines must be priced in {CurrencyCode}.");
        _items.Add(item);
    }

    public void SetAddress(ShipToAddress shipToAddress)
    {
        ArgumentNullException.ThrowIfNull(shipToAddress, nameof(shipToAddress));
        EnsurePending();
        ShipToAddress = shipToAddress.Copy();
    }

    public void SetShippingCost(Money shippingCost)
    {
        ArgumentNullException.ThrowIfNull(shippingCost, nameof(shippingCost));
        EnsurePending();
        if (shippingCost.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"Shipping must be expressed in {CurrencyCode}.", nameof(shippingCost));
        if (shippingCost.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(shippingCost), "Shipping cannot be negative.");
        OrderSendCostAmount = shippingCost;
    }

    /// <summary>Applies a discount code, refusing to discount more than the order is worth.</summary>
    public void ApplyDiscountCode(DiscountCode discountCode)
    {
        ArgumentNullException.ThrowIfNull(discountCode, nameof(discountCode));
        EnsurePending();
        if (!discountCode.IsValidOn(DateTimeOffset.UtcNow))
            throw new InvalidOperationException($"Discount code {discountCode.Code} is not currently valid.");

        var subtotal = Sum(_items.Select(i => i.AmountPayable));
        var discount = discountCode.CalculateOn(subtotal);
        if (discount.Amount > subtotal.Amount)
            discount = subtotal;

        DiscountCodeId = discountCode.Id;
        DiscountCodeAmount = discount;
    }

    public void RemoveDiscountCode()
    {
        EnsurePending();
        DiscountCodeId = null;
        DiscountCodeAmount = Money.Zero(CurrencyCode);
    }

    public void SetFulfillmentSource(Guid warehouseId) => WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddPayment(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        EnsureCanAddChild(payment, payment.OrderId, _payments);

        if (Status is OrderStatus.Cancelled or OrderStatus.Completed)
            throw new InvalidOperationException($"A {Status} order cannot take payments.");
        _payments.Add(payment);
    }

    /// <summary>
    /// The moment a basket becomes an order and the payment window starts running. Reserving stock
    /// and telling the customer hang off this, not off <see cref="Confirm"/>, which only happens
    /// once the money has actually arrived.
    /// </summary>
    public void Place()
    {
        EnsurePending();
        if (_items.Count == 0)
            throw new InvalidOperationException("An order cannot be placed without lines.");
        Raise(new OrderPlaced(Id, CustomerId, Code, AmountPayable.Amount, CurrencyCode));
    }

    public void Confirm()
    {
        EnsurePending();
        if (_items.Count == 0)
            throw new InvalidOperationException("An order cannot be confirmed without lines.");
        if (!IsFullyPaid)
            throw new InvalidOperationException($"The order still has {OutstandingAmount} outstanding.");
        Status = OrderStatus.Confirmed;
        RecordShipmentStatus(OrderShipmentStatus.OrderConfirmed);
        Raise(new OrderConfirmed(Id, CustomerId, Code));
    }

    public void RecordShipmentStatus(OrderShipmentStatus status)
    {
        if (Status is OrderStatus.Cancelled)
            throw new InvalidOperationException("A cancelled order has no shipment progress.");
        _shipmentStatusHistories.Add(new OrderShipmentStatusHistory(this, status));
    }

    public void Complete()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"An order in state {Status} cannot be completed.");
        Status = OrderStatus.Completed;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("A completed order cannot be cancelled; raise a return instead.");
        if (Status == OrderStatus.Cancelled)
            return;
        Status = OrderStatus.Cancelled;
        CancellationReason = reason.Trim();
        RecordShipmentStatusInternal(OrderShipmentStatus.OrderCancelled);
        Raise(new OrderCancelled(Id, Code, CancellationReason));
    }

    public OrderShipmentStatus? CurrentShipmentStatus =>
        _shipmentStatusHistories.OrderByDescending(h => h.ChangedDateTime).FirstOrDefault()?.Status;

    public bool HasPaymentExpired(DateTimeOffset asOf) =>
        Status == OrderStatus.Pending && TimeLeftToPayment != default && asOf > TimeLeftToPayment;

    private void RecordShipmentStatusInternal(OrderShipmentStatus status) =>
        _shipmentStatusHistories.Add(new OrderShipmentStatusHistory(this, status));

    private Money Sum(IEnumerable<Money> amounts)
    {
        var list = amounts.ToList();
        return list.Count == 0 ? Money.Zero(CurrencyCode) : list.Aggregate((accumulated, next) => accumulated.Add(next));
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"The order is {Status} and can no longer be edited.");
    }
}
