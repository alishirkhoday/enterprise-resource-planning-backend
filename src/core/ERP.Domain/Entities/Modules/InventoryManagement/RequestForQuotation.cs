namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// An invitation sent to several suppliers to quote for the same list of items, so their offers
/// can be compared on equal terms.
/// </summary>
public sealed partial class RequestForQuotation : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public DateOnly IssueDate { get; private set; }
    public DateOnly ResponseDeadline { get; private set; }
    public RequestForQuotationStatus Status { get; private set; }

    public Guid? RequisitionId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? Terms { get; private set; }
    public string? Description { get; private set; }

    /// <summary>The quotation that was chosen, once the comparison is settled.</summary>
    public Guid? AwardedQuotationId { get; private set; }

    private readonly List<RequestForQuotationItem> _items = [];
    public IReadOnlyList<RequestForQuotationItem> Items => _items.AsReadOnly();

    private readonly List<Supplier> _invitedSuppliers = [];
    public IReadOnlyList<Supplier> InvitedSuppliers => _invitedSuppliers.AsReadOnly();

    private RequestForQuotation() { }

    public RequestForQuotation(string number, string title, DateOnly issueDate, DateOnly responseDeadline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        if (responseDeadline < issueDate)
            throw new ArgumentOutOfRangeException(nameof(responseDeadline), "The deadline cannot precede the issue date.");

        Number = number.Trim();
        Title = title.Trim();
        IssueDate = issueDate;
        ResponseDeadline = responseDeadline;
        Status = RequestForQuotationStatus.Draft;
    }

    public void SetSource(Guid requisitionId) => RequisitionId = requisitionId == Guid.Empty ? null : requisitionId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetTerms(string? terms) => Terms = string.IsNullOrWhiteSpace(terms) ? null : terms.Trim();

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(RequestForQuotationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.RequestId, _items);

        EnsureDraft();
        _items.Add(item);
    }

    public void InviteSupplier(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        EnsureDraft();
        if (!supplier.CanBeOrderedFrom)
            throw new InvalidOperationException($"Supplier {supplier.Name} is {supplier.Status} and cannot be invited.");
        if (_invitedSuppliers.Any(s => s.Id == supplier.Id))
            return;
        _invitedSuppliers.Add(supplier);
    }

    public void Issue()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A request for quotation needs at least one line.");
        if (_invitedSuppliers.Count == 0)
            throw new InvalidOperationException("A request for quotation needs at least one invited supplier.");
        Status = RequestForQuotationStatus.Issued;
    }

    public void Close()
    {
        if (Status != RequestForQuotationStatus.Issued)
            throw new InvalidOperationException($"Only an issued request can be closed; this one is {Status}.");
        Status = RequestForQuotationStatus.Closed;
    }

    public void Award(Guid quotationId)
    {
        if (Status is not (RequestForQuotationStatus.Issued or RequestForQuotationStatus.Closed))
            throw new InvalidOperationException($"A request in state {Status} cannot be awarded.");
        if (quotationId == Guid.Empty)
            throw new ArgumentException("QuotationId is empty.", nameof(quotationId));
        AwardedQuotationId = quotationId;
        Status = RequestForQuotationStatus.Awarded;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == RequestForQuotationStatus.Awarded)
            throw new InvalidOperationException("An awarded request cannot be cancelled.");
        Status = RequestForQuotationStatus.Cancelled;
        Description = reason.Trim();
    }

    public bool IsOpenOn(DateOnly date) => Status == RequestForQuotationStatus.Issued && date <= ResponseDeadline;

    private void EnsureDraft()
    {
        if (Status != RequestForQuotationStatus.Draft)
            throw new InvalidOperationException($"The request is {Status} and can no longer be edited.");
    }
}
