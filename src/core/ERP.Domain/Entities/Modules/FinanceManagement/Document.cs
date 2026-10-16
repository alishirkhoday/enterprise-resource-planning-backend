namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A journal entry. It is the only way a balance ever changes, and it may not be posted unless
/// debits equal credits.
/// </summary>
public sealed class Document : BaseEntity, IBranchScoped
{
    public FinancialYear FinancialYear { get; private set; } = default!;
    public Guid FinancialYearId { get; private set; }

    public Guid? AccountingPeriodId { get; private set; }

    /// <summary>The sequential number the entry is known by once posted.</summary>
    public string? Number { get; private set; }

    public string Title { get; private set; } = default!;
    public DateTimeOffset FinancialEventDateTime { get; private set; }
    public string Description { get; private set; } = default!;
    public DocumentType Type { get; private set; }
    public DocumentStatus Status { get; private set; }
    public bool IsAutomatic { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public Guid? BranchId { get; private set; }

    /// <summary>What produced this entry, for example SalesInvoice together with its identifier.</summary>
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }

    public DateTimeOffset? PostedOn { get; private set; }
    public string? PostedBy { get; private set; }

    /// <summary>The entry that reverses this one, when it has been reversed.</summary>
    public Guid? ReversedByDocumentId { get; private set; }
    public Guid? ReversalOfDocumentId { get; private set; }

    private readonly List<DocumentItem> _items = [];
    public IReadOnlyList<DocumentItem> Items => _items;

    private Document() { }

    public Document(FinancialYear financialYear, string title, DateTimeOffset financialEventDateTime, string description, DocumentType type, bool isAutomatic = false)
    {
        ArgumentNullException.ThrowIfNull(financialYear, nameof(financialYear));
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));
        ArgumentException.ThrowIfNullOrEmpty(description, nameof(description));

        var eventDate = DateOnly.FromDateTime(financialEventDateTime.UtcDateTime);
        if (!financialYear.Contains(eventDate))
            throw new ArgumentOutOfRangeException(nameof(financialEventDateTime), $"The event date falls outside financial year {financialYear.Title}.");

        FinancialYear = financialYear;
        FinancialYearId = financialYear.Id;
        Title = title.Trim();
        FinancialEventDateTime = financialEventDateTime;
        Description = description.Trim();
        Type = type;
        Status = DocumentStatus.Draft;
        IsAutomatic = isAutomatic;
        CurrencyCode = financialYear.CurrencyCode;
        AccountingPeriodId = financialYear.PeriodOn(eventDate)?.Id;
    }

    public void Update(string title, DateTimeOffset financialEventDateTime, string description, DocumentType type)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));
        ArgumentException.ThrowIfNullOrEmpty(description, nameof(description));
        Title = title.Trim();
        FinancialEventDateTime = financialEventDateTime;
        Description = description.Trim();
        Type = type;
    }

    public void SetNumber(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        Number = number.Trim();
    }

    public void SetReference(string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddItem(DocumentItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        if (!item.Account.AcceptsPostings)
            throw new InvalidOperationException($"Account {item.Account.Code} does not accept postings.");
        if (item.Amount.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"The line currency {item.Amount.CurrencyCode} does not match the entry currency {CurrencyCode}.");
        _items.Add(item);
    }

    public void RemoveItem(DocumentItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Remove(item);
    }

    public decimal TotalDebtor => _items.Sum(i => i.Debtor);
    public decimal TotalCreditor => _items.Sum(i => i.Creditor);

    public bool IsBalanced() => TotalDebtor == TotalCreditor;

    /// <summary>
    /// Makes the entry final. Once posted it can only be reversed, never edited, which is what
    /// makes the ledger trustworthy.
    /// </summary>
    public void Post(string? postedBy = null)
    {
        EnsureDraft();
        if (_items.Count < 2)
            throw new InvalidOperationException("A journal entry needs at least two lines.");
        if (!IsBalanced())
            throw new InvalidOperationException($"The entry is out of balance: debits are {TotalDebtor} and credits are {TotalCreditor}.");
        if (!FinancialYear.AcceptsPostings)
            throw new InvalidOperationException($"Financial year {FinancialYear.Title} does not accept postings.");

        Status = DocumentStatus.Definite;
        PostedOn = DateTimeOffset.UtcNow;
        PostedBy = postedBy;
        Raise(new JournalEntryPosted(Id, FinancialYearId, Number, TotalDebtor));
    }

    /// <summary>Records that another entry reverses this one.</summary>
    public void MarkAsReversed(Guid reversingDocumentId)
    {
        if (Status != DocumentStatus.Definite)
            throw new InvalidOperationException("Only a posted entry can be reversed.");
        if (ReversedByDocumentId is not null)
            throw new InvalidOperationException("This entry has already been reversed.");
        ReversedByDocumentId = reversingDocumentId;
        Raise(new JournalEntryReversed(Id, reversingDocumentId));
    }

    public void MarkAsReversalOf(Guid originalDocumentId) => ReversalOfDocumentId = originalDocumentId;

    public void ChangeStatus(DocumentStatus status) => Status = status;

    private void EnsureDraft()
    {
        if (Status != DocumentStatus.Draft)
            throw new InvalidOperationException("A posted journal entry cannot be modified; reverse it instead.");
    }
}
