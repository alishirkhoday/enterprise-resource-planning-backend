namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One supplier answer to a request for quotation. Accepting it is what a purchase order is
/// normally built from.
/// </summary>
public sealed class SupplierQuotation : BaseEntity
{
    public string Number { get; private set; } = default!;

    public Supplier Supplier { get; private set; } = default!;
    public Guid SupplierId { get; private set; }

    public Guid? RequestForQuotationId { get; private set; }

    public DateOnly QuotationDate { get; private set; }
    public DateOnly? ValidUntil { get; private set; }
    public SupplierQuotationStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public Guid? PaymentTermId { get; private set; }
    public int? LeadTimeDays { get; private set; }

    /// <summary>Freight and other costs the supplier adds to the quotation as a whole.</summary>
    public Money AdditionalCosts { get; private set; } = default!;

    /// <summary>The reference the supplier gives their own offer.</summary>
    public string? SupplierReference { get; private set; }
    public string? Terms { get; private set; }
    public string? Description { get; private set; }

    private readonly List<SupplierQuotationItem> _items = [];
    public IReadOnlyList<SupplierQuotationItem> Items => _items;

    private SupplierQuotation() { }

    public SupplierQuotation(string number, Supplier supplier, DateOnly quotationDate, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Number = number.Trim();
        Supplier = supplier;
        SupplierId = supplier.Id;
        QuotationDate = quotationDate;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        AdditionalCosts = Money.Zero(CurrencyCode);
        Status = SupplierQuotationStatus.Received;
    }

    public void LinkToRequest(Guid requestForQuotationId) =>
        RequestForQuotationId = requestForQuotationId == Guid.Empty ? null : requestForQuotationId;

    public void SetValidity(DateOnly validUntil)
    {
        if (validUntil < QuotationDate)
            throw new ArgumentOutOfRangeException(nameof(validUntil), "The validity cannot end before the quotation date.");
        ValidUntil = validUntil;
    }

    public void SetCommercialTerms(Guid? paymentTermId, int? leadTimeDays, string? terms)
    {
        if (leadTimeDays.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(leadTimeDays.Value, nameof(leadTimeDays));
        PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
        LeadTimeDays = leadTimeDays;
        Terms = string.IsNullOrWhiteSpace(terms) ? null : terms.Trim();
    }

    public void SetSupplierReference(string? supplierReference) =>
        SupplierReference = string.IsNullOrWhiteSpace(supplierReference) ? null : supplierReference.Trim();

    public void SetAdditionalCosts(Money additionalCosts)
    {
        ArgumentNullException.ThrowIfNull(additionalCosts, nameof(additionalCosts));
        if (additionalCosts.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"Additional costs must be expressed in {CurrencyCode}.", nameof(additionalCosts));
        AdditionalCosts = additionalCosts;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(SupplierQuotationItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureOpen();
        if (item.UnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Quotation lines must be priced in {CurrencyCode}.");
        _items.Add(item);
    }

    public Money SubTotal() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.LineTotal).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money GrandTotal() => SubTotal().Add(AdditionalCosts);

    public void Accept()
    {
        EnsureOpen();
        Status = SupplierQuotationStatus.Accepted;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        EnsureOpen();
        Status = SupplierQuotationStatus.Rejected;
        Description = reason.Trim();
    }

    public void MarkAsExpired() => Status = SupplierQuotationStatus.Expired;

    public bool IsValidOn(DateOnly date) =>
        Status == SupplierQuotationStatus.Received && (ValidUntil is null || date <= ValidUntil.Value);

    private void EnsureOpen()
    {
        if (Status != SupplierQuotationStatus.Received)
            throw new InvalidOperationException($"The quotation is {Status} and can no longer change.");
    }
}
