using ERP.Domain.Entities.Modules.FinanceManagement.ValueObjects;

namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A financial year - سال مالی, and the periods inside it.
/// 
/// Nearly every document belongs to one, and which one it belongs to decides whether it can be
/// posted at all: a year that has been closed off takes nothing further.
/// </summary>
public sealed class FinancialYear : BaseEntity, IBranchScoped
{
    public string Title { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FinancialYearSetting Setting { get; private set; } = default!;

    public FinancialYearStatus Status { get; private set; }
    public DateTimeOffset? ClosedOn { get; private set; }
    public string? ClosedBy { get; private set; }

    /// <summary>The currency the books of this year are kept in.</summary>
    public string CurrencyCode { get; private set; } = default!;

    public Guid? BranchId { get; private set; }

    /// <summary>The journal entry that carries last year closing balances into this year.</summary>
    public Guid? OpeningDocumentId { get; private set; }

    /// <summary>The journal entry that zeroes the profit and loss accounts at year end.</summary>
    public Guid? ClosingDocumentId { get; private set; }

    private readonly List<Document> _documents = [];
    public IReadOnlyList<Document> Documents => _documents;

    private readonly List<AccountingPeriod> _periods = [];
    public IReadOnlyList<AccountingPeriod> Periods => _periods;

    private FinancialYear() { }

    public FinancialYear(string title, DateOnly startDate, DateOnly endDate, string currencyCode = "IRR")
    {
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (endDate < startDate)
            throw new ArgumentOutOfRangeException(nameof(endDate), $"EndDate cannot be earlier than StartDate.");
        Title = title.Trim();
        StartDate = startDate;
        EndDate = endDate;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Status = FinancialYearStatus.Draft;
    }

    public void SetSetting(FinancialYearSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting, nameof(setting));
        Setting = setting;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddPeriod(AccountingPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        if (_periods.Any(p => p.Number == period.Number))
            throw new InvalidOperationException($"Period number {period.Number} already exists in {Title}.");
        if (_periods.Any(p => p.Period.Overlaps(period.Period)))
            throw new InvalidOperationException("The period overlaps an existing period of this year.");
        _periods.Add(period);
    }

    public AccountingPeriod? PeriodOn(DateOnly date) => _periods.FirstOrDefault(p => p.Contains(date));

    public void Open(Guid openingDocumentId)
    {
        if (Status != FinancialYearStatus.Draft)
            throw new InvalidOperationException($"Only a draft year can be opened; this one is {Status}.");
        if (_periods.Count == 0)
            throw new InvalidOperationException("A financial year cannot be opened before its periods are defined.");
        OpeningDocumentId = openingDocumentId == Guid.Empty ? null : openingDocumentId;
        Status = FinancialYearStatus.Open;
    }

    /// <summary>Moves the year into the closing phase, where only adjusting entries are accepted.</summary>
    public void BeginClosing()
    {
        if (Status != FinancialYearStatus.Open)
            throw new InvalidOperationException($"Only an open year can begin closing; this one is {Status}.");
        Status = FinancialYearStatus.Closing;
    }

    public void Close(Guid closingDocumentId, string? closedBy = null)
    {
        if (Status != FinancialYearStatus.Closing)
            throw new InvalidOperationException("The year must be in the closing phase before it can be closed.");
        if (_periods.Any(p => p.Status != AccountingPeriodStatus.Closed))
            throw new InvalidOperationException("Every period must be closed before the financial year can be closed.");
        if (_documents.Any(d => d.Status == DocumentStatus.Draft))
            throw new InvalidOperationException("Draft journal entries must be posted or removed before closing the year.");

        ClosingDocumentId = closingDocumentId == Guid.Empty ? null : closingDocumentId;
        Status = FinancialYearStatus.Closed;
        ClosedOn = DateTimeOffset.UtcNow;
        ClosedBy = closedBy;
        Raise(new FinancialYearClosed(Id, Title));
    }

    public bool AcceptsPostings => Status is FinancialYearStatus.Open or FinancialYearStatus.Closing;

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;
}
