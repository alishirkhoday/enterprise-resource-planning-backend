namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A month, or any other sub-interval, of a financial year. Closing a period is what stops
/// anyone posting into a month that has already been reported.
/// </summary>
public sealed class AccountingPeriod : BaseEntity
{
    public FinancialYear FinancialYear { get; private set; } = default!;
    public Guid FinancialYearId { get; private set; }

    public string Title { get; private set; } = default!;

    /// <summary>Sequence within the year, starting at one.</summary>
    public short Number { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public AccountingPeriodStatus Status { get; private set; }
    public DateTimeOffset? ClosedOn { get; private set; }
    public string? ClosedBy { get; private set; }

    private AccountingPeriod() { }

    public AccountingPeriod(FinancialYear financialYear, short number, string title, DateRange period)
    {
        ArgumentNullException.ThrowIfNull(financialYear, nameof(financialYear));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number, nameof(number));
        if (period.EndDate is null)
            throw new ArgumentException("An accounting period must have an end date.", nameof(period));
        if (period.StartDate < financialYear.StartDate || period.EndDate.Value > financialYear.EndDate)
            throw new ArgumentOutOfRangeException(nameof(period), "The period must fall inside its financial year.");

        FinancialYear = financialYear;
        FinancialYearId = financialYear.Id;
        Number = number;
        Title = title.Trim();
        Period = period;
        Status = AccountingPeriodStatus.Open;
    }

    internal void Close(string? closedBy = null)
    {
        if (Status == AccountingPeriodStatus.Closed)
            throw new InvalidOperationException($"Period {Title} is already closed.");
        Status = AccountingPeriodStatus.Closed;
        ClosedOn = DateTimeOffset.UtcNow;
        ClosedBy = closedBy;
        Raise(new AccountingPeriodClosed(Id, FinancialYearId));
    }

    /// <summary>Reopens a closed period, which auditors expect to be a deliberate and rare act.</summary>
    internal void Reopen(string? reopenedBy = null)
    {
        if (Status != AccountingPeriodStatus.Closed)
            throw new InvalidOperationException($"Period {Title} is not closed.");
        if (FinancialYear.Status == FinancialYearStatus.Closed)
            throw new InvalidOperationException("A period of a closed financial year cannot be reopened.");
        Status = AccountingPeriodStatus.Open;
        ClosedOn = null;
        ClosedBy = reopenedBy;
    }

    /// <summary>Allows reading but refuses new postings, used while a period is being reviewed.</summary>
    internal void Lock()
    {
        if (Status == AccountingPeriodStatus.Closed)
            throw new InvalidOperationException("A closed period is already beyond locking.");
        Status = AccountingPeriodStatus.Locked;
    }

    internal void Unlock()
    {
        if (Status != AccountingPeriodStatus.Locked)
            throw new InvalidOperationException($"Period {Title} is not locked.");
        Status = AccountingPeriodStatus.Open;
    }

    public bool AcceptsPostings => Status == AccountingPeriodStatus.Open;

    public bool Contains(DateOnly date) => Period.Contains(date);
}
