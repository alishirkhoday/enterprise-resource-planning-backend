namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// The supplier bill. Posting it is what creates the payable, and it is matched against the
/// order and the receipt so nobody is paid for what never arrived.
/// </summary>
public sealed partial class PurchaseInvoice : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;

    /// <summary>The number the supplier printed on their own invoice.</summary>
    public string SupplierInvoiceNumber { get; private set; } = default!;

    public Supplier Supplier { get; private set; } = default!;
    public Guid SupplierId { get; private set; }

    public Guid? PurchaseOrderId { get; private set; }
    public Guid? GoodsReceiptId { get; private set; }

    public DateOnly InvoiceDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public PurchaseInvoiceStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public decimal ExchangeRate { get; private set; }

    public Money AdditionalCosts { get; private set; } = default!;
    public Guid? PaymentTermId { get; private set; }
    public Guid? PayableId { get; private set; }
    public Guid? DocumentId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? Description { get; private set; }

    private readonly List<PurchaseInvoiceItem> _items = [];
    public IReadOnlyList<PurchaseInvoiceItem> Items => _items.AsReadOnly();

    private PurchaseInvoice() { }

    public PurchaseInvoice(string number, string supplierInvoiceNumber, Supplier supplier, DateOnly invoiceDate, DateOnly dueDate, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierInvoiceNumber, nameof(supplierInvoiceNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (dueDate < invoiceDate)
            throw new ArgumentOutOfRangeException(nameof(dueDate), "The due date cannot precede the invoice date.");

        Number = number.Trim();
        SupplierInvoiceNumber = supplierInvoiceNumber.Trim();
        Supplier = supplier;
        SupplierId = supplier.Id;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ExchangeRate = 1m;
        AdditionalCosts = Money.Zero(CurrencyCode);
        Status = PurchaseInvoiceStatus.Draft;
    }

    public void LinkToOrder(Guid purchaseOrderId, Guid? goodsReceiptId = null)
    {
        PurchaseOrderId = purchaseOrderId == Guid.Empty ? null : purchaseOrderId;
        GoodsReceiptId = goodsReceiptId == Guid.Empty ? null : goodsReceiptId;
    }

    public void SetExchangeRate(decimal exchangeRate)
    {
        EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exchangeRate, nameof(exchangeRate));
        ExchangeRate = exchangeRate;
    }

    public void SetAdditionalCosts(Money additionalCosts)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(additionalCosts, nameof(additionalCosts));
        if (additionalCosts.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"Additional costs must be expressed in {CurrencyCode}.", nameof(additionalCosts));
        AdditionalCosts = additionalCosts;
    }

    public void SetPaymentTerm(Guid paymentTermId) => PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(PurchaseInvoiceItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.InvoiceId, _items);

        EnsureDraft();
        if (item.UnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Invoice lines must be priced in {CurrencyCode}.");
        _items.Add(item);
    }

    public Money SubTotal() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.LineTotal).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money TotalTax() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.TaxAmount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money GrandTotal() => SubTotal().Add(TotalTax()).Add(AdditionalCosts);

    /// <summary>Flags the invoice as disputed, which holds it back from payment.</summary>
    public void Dispute(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == PurchaseInvoiceStatus.Paid)
            throw new InvalidOperationException("A paid invoice cannot be disputed here; raise a credit note.");
        Status = PurchaseInvoiceStatus.Disputed;
        Description = reason.Trim();
    }

    public void Post(Guid payableId, Guid? documentId = null)
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("An invoice cannot be posted without lines.");
        PayableId = payableId == Guid.Empty ? null : payableId;
        DocumentId = documentId == Guid.Empty ? null : documentId;
        Status = PurchaseInvoiceStatus.Posted;
    }

    public void MarkAsPaid()
    {
        if (Status != PurchaseInvoiceStatus.Posted)
            throw new InvalidOperationException("Only a posted invoice can be marked as paid.");
        Status = PurchaseInvoiceStatus.Paid;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == PurchaseInvoiceStatus.Paid)
            throw new InvalidOperationException("A paid invoice cannot be cancelled.");
        Status = PurchaseInvoiceStatus.Cancelled;
        Description = reason.Trim();
    }

    private void EnsureDraft()
    {
        if (Status is not (PurchaseInvoiceStatus.Draft or PurchaseInvoiceStatus.Disputed))
            throw new InvalidOperationException($"The invoice is {Status} and can no longer be edited.");
    }
}
