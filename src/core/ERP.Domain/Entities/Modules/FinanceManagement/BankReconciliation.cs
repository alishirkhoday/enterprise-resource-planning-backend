namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Compares the bank statement against the ledger for one period and records what explains the gap.
/// It may only be completed once the unexplained difference is zero.
/// </summary>
public sealed class BankReconciliation : BaseEntity
{
    public BankBranchAccount BankBranchAccount { get; private set; } = default!;
    public Guid BankBranchAccountId { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public DateOnly StatementDate { get; private set; }

    /// <summary>The closing balance the bank reports.</summary>
    public Money StatementBalance { get; private set; } = default!;

    /// <summary>The closing balance the ledger holds.</summary>
    public Money BookBalance { get; private set; } = default!;

    public BankReconciliationStatus Status { get; private set; }
    public DateTimeOffset? CompletedOn { get; private set; }
    public string? CompletedBy { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<BankReconciliationItem> _items = [];
    public IReadOnlyList<BankReconciliationItem> Items => _items;

    private BankReconciliation() { }

    public BankReconciliation(BankBranchAccount bankBranchAccount, DateRange period, DateOnly statementDate, Money statementBalance, Money bookBalance)
    {
        ArgumentNullException.ThrowIfNull(bankBranchAccount, nameof(bankBranchAccount));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentNullException.ThrowIfNull(statementBalance, nameof(statementBalance));
        ArgumentNullException.ThrowIfNull(bookBalance, nameof(bookBalance));
        if (statementBalance.CurrencyCode != bookBalance.CurrencyCode)
            throw new ArgumentException("Both balances must be expressed in one currency.", nameof(bookBalance));

        BankBranchAccount = bankBranchAccount;
        BankBranchAccountId = bankBranchAccount.Id;
        Period = period;
        StatementDate = statementDate;
        StatementBalance = statementBalance;
        BookBalance = bookBalance;
        Status = BankReconciliationStatus.InProgress;
    }

    public void AddItem(BankReconciliationItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureInProgress();
        _items.Add(item);
    }

    public void RemoveItem(BankReconciliationItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureInProgress();
        _items.Remove(item);
    }

    /// <summary>The book balance adjusted by every reconciling item.</summary>
    public Money AdjustedBookBalance()
    {
        var adjusted = BookBalance;
        foreach (var item in _items)
        {
            adjusted = adjusted.Add(item.SignedAmount());
        }
        return adjusted;
    }

    /// <summary>What is still unexplained. It must reach zero before the reconciliation can close.</summary>
    public Money UnexplainedDifference() => StatementBalance.Subtract(AdjustedBookBalance());

    public void Complete(string? completedBy = null)
    {
        EnsureInProgress();
        var difference = UnexplainedDifference();
        if (!difference.IsZero)
            throw new InvalidOperationException($"The reconciliation still has an unexplained difference of {difference}.");

        Status = BankReconciliationStatus.Completed;
        CompletedOn = DateTimeOffset.UtcNow;
        CompletedBy = completedBy;
    }

    public void Reopen()
    {
        if (Status != BankReconciliationStatus.Completed)
            throw new InvalidOperationException("Only a completed reconciliation can be reopened.");
        Status = BankReconciliationStatus.InProgress;
        CompletedOn = null;
    }

    public void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    private void EnsureInProgress()
    {
        if (Status != BankReconciliationStatus.InProgress)
            throw new InvalidOperationException($"The reconciliation is {Status} and can no longer be edited.");
    }
}
