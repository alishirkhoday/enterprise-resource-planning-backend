namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// A bill issued to a customer. Posting it is what creates the receivable and the output tax;
/// a draft commits to nothing.
/// </summary>
public sealed partial class Invoice : AggregateRoot, IBranchScoped
{
    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public string Number { get; private set; } = default!;
    public InvoiceType Type { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public DateTimeOffset RegistrationDateTime { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public decimal ExchangeRate { get; private set; }

    public Guid? SaleOrderId { get; private set; }
    public Guid? PaymentTermId { get; private set; }
    public Guid? ReceivableId { get; private set; }
    public Guid? DocumentId { get; private set; }
    public Guid? SalesRepHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>The invoice this one corrects, for a credit or debit note.</summary>
    public Guid? OriginalInvoiceId { get; private set; }

    /// <summary>Freight and handling charged on the invoice as a whole.</summary>
    public Money AdditionalCharges { get; private set; } = default!;

    /// <summary>A discount applied to the whole invoice, on top of any line discounts.</summary>
    public Percentage HeaderDiscountPercentage { get; private set; } = default!;

    /// <summary>The identifier the tax authority returns when the invoice is filed.</summary>
    public string? TaxAuthorityReference { get; private set; }

    public string? Description { get; private set; }

    private readonly List<InvoiceItem> _items = [];
    public IReadOnlyList<InvoiceItem> Items => _items.AsReadOnly();

    private Invoice() { }

    public Invoice(Customer customer, string number, InvoiceType type, DateOnly issueDate, DateOnly dueDate, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (dueDate < issueDate)
            throw new ArgumentOutOfRangeException(nameof(dueDate), "The due date cannot precede the issue date.");

        Customer = customer;
        CustomerId = customer.Id;
        Number = number.Trim();
        Type = type;
        IssueDate = issueDate;
        DueDate = dueDate;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ExchangeRate = 1m;
        RegistrationDateTime = DateTimeOffset.UtcNow;
        Status = InvoiceStatus.Draft;
        AdditionalCharges = Money.Zero(CurrencyCode);
        HeaderDiscountPercentage = Percentage.Zero();
    }

    public void SetExchangeRate(decimal exchangeRate)
    {
        EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exchangeRate, nameof(exchangeRate));
        ExchangeRate = exchangeRate;
    }

    public void LinkToSaleOrder(Guid saleOrderId) => SaleOrderId = saleOrderId == Guid.Empty ? null : saleOrderId;

    public void LinkToOriginalInvoice(Guid originalInvoiceId)
    {
        if (Type is not (InvoiceType.CreditNote or InvoiceType.DebitNote))
            throw new InvalidOperationException("Only a credit or debit note refers back to another invoice.");
        OriginalInvoiceId = originalInvoiceId == Guid.Empty ? null : originalInvoiceId;
    }

    public void SetPaymentTerm(Guid paymentTermId, DateOnly dueDate)
    {
        EnsureDraft();
        if (dueDate < IssueDate)
            throw new ArgumentOutOfRangeException(nameof(dueDate), "The due date cannot precede the issue date.");
        PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
        DueDate = dueDate;
    }

    public void AssignSalesRep(Guid humanId) => SalesRepHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetAdditionalCharges(Money additionalCharges)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(additionalCharges, nameof(additionalCharges));
        if (additionalCharges.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"Additional charges must be expressed in {CurrencyCode}.", nameof(additionalCharges));
        AdditionalCharges = additionalCharges;
    }

    public void SetHeaderDiscount(Percentage headerDiscountPercentage)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(headerDiscountPercentage, nameof(headerDiscountPercentage));
        HeaderDiscountPercentage = headerDiscountPercentage;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(InvoiceItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.InvoiceId, _items);

        EnsureDraft();
        if (item.ItemUnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Invoice lines must be priced in {CurrencyCode}.");
        _items.Add(item);
    }

    public void RemoveItem(InvoiceItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.InvoiceId != Id || !_items.Contains(item))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Remove(item);
    }

    public Money TotalAmount =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.Amount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money TotalLineDiscounts =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.Discount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money NetOfLineDiscounts =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.AmountPayable).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money HeaderDiscountAmount => NetOfLineDiscounts.Percent(HeaderDiscountPercentage);

    public Money TotalDiscounts => TotalLineDiscounts.Add(HeaderDiscountAmount);

    public Money TaxableAmount => NetOfLineDiscounts.Subtract(HeaderDiscountAmount);

    public Money TotalTax =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.TaxAmount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money AmountPayable => TaxableAmount.Add(TotalTax).Add(AdditionalCharges);

    public void SetItems(IEnumerable<InvoiceItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        EnsureDraft();
        var replacement = new List<InvoiceItem>();
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            EnsureCanAddChild(item, item.InvoiceId, replacement);
            if (item.ItemUnitPrice.CurrencyCode != CurrencyCode)
                throw new InvalidOperationException($"Lines must be priced in {CurrencyCode}.");
            replacement.Add(item);
        }
        _items.Clear();
        _items.AddRange(replacement);
    }

    public void Issue()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("An invoice cannot be issued without lines.");
        // A credit note hands money back, so it is allowed even when the customer is blocked:
        // often the block is exactly why the goods came back.
        if (Type != InvoiceType.CreditNote && !Customer.CanBeSoldTo)
            throw new InvalidOperationException($"Customer {Customer.DisplayName} is {Customer.Status}.");
        Status = InvoiceStatus.Issued;
    }

    public void Post(Guid? receivableId, Guid? documentId)
    {
        if (Status != InvoiceStatus.Issued)
            throw new InvalidOperationException($"Only an issued invoice can be posted; this one is {Status}.");
        if (Type != InvoiceType.Proforma)
            ReceivableId = receivableId == Guid.Empty ? null : receivableId;
        DocumentId = documentId == Guid.Empty ? null : documentId;
        Status = InvoiceStatus.Posted;
        Raise(new InvoicePosted(Id, CustomerId, Number, AmountPayable.Amount, CurrencyCode));
    }

    public void RecordTaxAuthorityReference(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference, nameof(reference));
        TaxAuthorityReference = reference.Trim();
    }

    public void MarkAsPaid()
    {
        if (Status != InvoiceStatus.Posted)
            throw new InvalidOperationException("Only a posted invoice can be marked as paid.");
        Status = InvoiceStatus.Paid;
    }

    public void MarkAsPartiallyPaid()
    {
        if (Status is not (InvoiceStatus.Posted or InvoiceStatus.PartiallyPaid))
            throw new InvalidOperationException("Only a posted invoice can be partly paid.");
        Status = InvoiceStatus.PartiallyPaid;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status is InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("A paid invoice cannot be cancelled; raise a credit note.");
        Status = InvoiceStatus.Cancelled;
        Description = reason.Trim();
    }

    public bool IsOverdueOn(DateOnly date) =>
        Status is InvoiceStatus.Posted or InvoiceStatus.PartiallyPaid && date > DueDate;

    private void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"The invoice is {Status} and can no longer be edited.");
    }
}
