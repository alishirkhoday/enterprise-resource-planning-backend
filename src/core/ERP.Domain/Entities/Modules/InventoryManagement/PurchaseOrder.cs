namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A commitment to buy from a supplier at agreed prices. It is the document goods receipts and
/// purchase invoices are both matched against.
/// </summary>
public sealed partial class PurchaseOrder : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;

    public Supplier Supplier { get; private set; } = default!;
    public Guid SupplierId { get; private set; }

    public DateTimeOffset OrderDateTime { get; private set; }
    public DateOnly? ExpectedDeliveryDate { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public decimal ExchangeRate { get; private set; }

    public Guid? WarehouseId { get; private set; }
    public Guid? PaymentTermId { get; private set; }
    public Guid? RequisitionId { get; private set; }
    public Guid? QuotationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? ApprovalRequestId { get; private set; }

    /// <summary>Freight, insurance and other costs that attach to the order as a whole.</summary>
    public Money AdditionalCosts { get; private set; } = default!;

    public string? Description { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset? SubmittedOn { get; private set; }
    public DateTimeOffset? ApprovedOn { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly List<PurchaseOrderItem> _items = [];
    public IReadOnlyList<PurchaseOrderItem> Items => _items.AsReadOnly();

    private PurchaseOrder() { }

    public PurchaseOrder(string number, Supplier supplier, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        if (!supplier.CanBeOrderedFrom)
            throw new InvalidOperationException($"Supplier {supplier.Name} is {supplier.Status} and cannot be ordered from.");

        Number = number.Trim();
        Supplier = supplier;
        SupplierId = supplier.Id;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ExchangeRate = 1m;
        OrderDateTime = DateTimeOffset.UtcNow;
        Status = PurchaseOrderStatus.Draft;
        AdditionalCosts = Money.Zero(CurrencyCode);
    }

    public void SetExpectedDeliveryDate(DateOnly expectedDeliveryDate)
    {
        EnsureDraft();
        ExpectedDeliveryDate = expectedDeliveryDate;
    }

    public void SetExchangeRate(decimal exchangeRate)
    {
        EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exchangeRate, nameof(exchangeRate));
        ExchangeRate = exchangeRate;
    }

    public void SetDeliveryTarget(Guid warehouseId) => WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;

    public void SetPaymentTerm(Guid paymentTermId) => PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;

    public void SetSource(Guid? requisitionId, Guid? quotationId)
    {
        RequisitionId = requisitionId == Guid.Empty ? null : requisitionId;
        QuotationId = quotationId == Guid.Empty ? null : quotationId;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void SetAdditionalCosts(Money additionalCosts)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(additionalCosts, nameof(additionalCosts));
        if (additionalCosts.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"Additional costs must be expressed in {CurrencyCode}.", nameof(additionalCosts));
        if (additionalCosts.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(additionalCosts), "Additional costs cannot be negative.");
        AdditionalCosts = additionalCosts;
    }

    public void AddItem(PurchaseOrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.PurchaseOrderId, _items);

        EnsureDraft();
        if (item.UnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Order lines must be priced in {CurrencyCode}.");
        if (!item.Item.IsPurchasable)
            throw new InvalidOperationException($"Item {item.Item.Name} is not marked as purchasable.");
        _items.Add(item);
    }

    public void RemoveItem(PurchaseOrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.PurchaseOrderId != Id || !_items.Contains(item))
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

    public Money GrandTotal() => SubTotal().Add(TotalTax()).Add(AdditionalCosts);

    public void Submit()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A purchase order cannot be submitted without lines.");
        Status = PurchaseOrderStatus.Submitted;
        SubmittedOn = DateTimeOffset.UtcNow;
    }

    public void Approve(Guid? approvalRequestId = null)
    {
        if (Status != PurchaseOrderStatus.Submitted)
            throw new InvalidOperationException($"Only a submitted order can be approved; this one is {Status}.");
        ApprovalRequestId = approvalRequestId == Guid.Empty ? null : approvalRequestId;
        ApprovedOn = DateTimeOffset.UtcNow;
        Status = PurchaseOrderStatus.Approved;
        Raise(new PurchaseOrderApproved(Id, Number, SupplierId));
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != PurchaseOrderStatus.Submitted)
            throw new InvalidOperationException($"Only a submitted order can be rejected; this one is {Status}.");
        Status = PurchaseOrderStatus.Draft;
        Note = reason.Trim();
    }

    /// <summary>
    /// Recomputes the delivery state from what the lines have received, so the order status is
    /// always a consequence of the receipts rather than something set by hand.
    /// </summary>
    public void RefreshReceiptStatus()
    {
        if (Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Submitted or PurchaseOrderStatus.Cancelled)
            return;

        if (_items.All(i => i.IsFullyReceived))
            Status = PurchaseOrderStatus.Received;
        else if (_items.Any(i => i.ReceivedQuantity > 0m))
            Status = PurchaseOrderStatus.PartiallyReceived;
        else
            Status = PurchaseOrderStatus.Approved;
    }

    /// <summary>Closes an order that will not be fully delivered, accepting what arrived.</summary>
    public void CloseShort(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException($"An order in state {Status} cannot be closed short.");
        Status = PurchaseOrderStatus.Closed;
        Note = reason.Trim();
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (_items.Any(i => i.ReceivedQuantity > 0m))
            throw new InvalidOperationException("An order with receipts cannot be cancelled; close it short instead.");
        Status = PurchaseOrderStatus.Cancelled;
        CancellationReason = reason.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();

    public void SetNote(string? note) => Note = string.IsNullOrEmpty(note) ? null : note.Trim();

    public bool CanReceive => Status is PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived;

    private void EnsureDraft()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException($"The order is {Status} and can no longer be edited.");
    }
}
