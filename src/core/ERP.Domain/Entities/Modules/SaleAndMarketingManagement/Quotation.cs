using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// A priced offer made to a customer, valid for a period. Accepting it is what a sale order is
/// normally built from.
/// </summary>
public sealed partial class Quotation : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;

    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public DateTimeOffset IssueDate { get; private set; }
    public DateTimeOffset? ExpiryDate { get; private set; }
    public QuotationStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public int Version { get; private set; }

    public Guid? OpportunityId { get; private set; }
    public Guid? PaymentTermId { get; private set; }
    public Guid? PriceListId { get; private set; }
    public Guid? SalesRepHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>The order raised once the customer accepted.</summary>
    public Guid? SaleOrderId { get; private set; }

    public Money AdditionalCharges { get; private set; } = default!;
    public Percentage HeaderDiscountPercentage { get; private set; } = default!;

    public string? Terms { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? Description { get; private set; }

    private readonly List<QuotationItem> _items = [];
    public IReadOnlyList<QuotationItem> Items => _items.AsReadOnly();

    private Quotation() { }

    public Quotation(string number, Customer customer, DateTimeOffset issueDate, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Number = number.Trim();
        Customer = customer;
        CustomerId = customer.Id;
        IssueDate = issueDate;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Version = 1;
        Status = QuotationStatus.Draft;
        AdditionalCharges = Money.Zero(CurrencyCode);
        HeaderDiscountPercentage = Percentage.Zero();
    }

    public void SetExpiryDate(DateTimeOffset expiryDate)
    {
        if (expiryDate <= IssueDate)
            throw new ArgumentOutOfRangeException(nameof(expiryDate), "The expiry cannot precede the issue date.");
        ExpiryDate = expiryDate;
    }

    public void LinkToOpportunity(Guid opportunityId) => OpportunityId = opportunityId == Guid.Empty ? null : opportunityId;

    public void SetCommercialTerms(Guid? paymentTermId, Guid? priceListId, string? terms)
    {
        PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
        PriceListId = priceListId == Guid.Empty ? null : priceListId;
        Terms = string.IsNullOrWhiteSpace(terms) ? null : terms.Trim();
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

    public void AddItem(QuotationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.QuotationId, _items);

        EnsureDraft();
        if (item.UnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Quotation lines must be priced in {CurrencyCode}.");
        _items.Add(item);
    }

    public void RemoveItem(QuotationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.QuotationId != Id || !_items.Contains(item))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Remove(item);
    }

    public Money SubTotal() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.LineTotal).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money HeaderDiscountAmount() => SubTotal().Percent(HeaderDiscountPercentage);

    public Money TotalTax() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.TaxAmount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money GrandTotal() => SubTotal().Subtract(HeaderDiscountAmount()).Add(TotalTax()).Add(AdditionalCharges);

    public void Send()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A quotation cannot be sent without lines.");
        Status = QuotationStatus.Sent;
    }

    public void Accept(Guid? saleOrderId = null)
    {
        if (Status != QuotationStatus.Sent)
            throw new InvalidOperationException($"Only a sent quotation can be accepted; this one is {Status}.");
        SaleOrderId = saleOrderId == Guid.Empty ? null : saleOrderId;
        Status = QuotationStatus.Accepted;
        Raise(new QuotationAccepted(Id, CustomerId, Number));
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != QuotationStatus.Sent)
            throw new InvalidOperationException($"Only a sent quotation can be rejected; this one is {Status}.");
        Status = QuotationStatus.Rejected;
        RejectionReason = reason.Trim();
    }

    /// <summary>Opens a new version of the offer, leaving the sent one as history.</summary>
    public void Revise()
    {
        if (Status is QuotationStatus.Accepted)
            throw new InvalidOperationException("An accepted quotation cannot be revised.");
        Version++;
        Status = QuotationStatus.Draft;
        RejectionReason = null;
    }

    public void MarkAsExpired()
    {
        if (Status != QuotationStatus.Sent)
            return;
        Status = QuotationStatus.Expired;
    }

    public bool IsValidOn(DateTimeOffset moment) =>
        Status == QuotationStatus.Sent && (ExpiryDate is null || moment <= ExpiryDate.Value);

    private void EnsureDraft()
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException($"The quotation is {Status} and can no longer be edited; revise it instead.");
    }
}
