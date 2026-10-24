namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// One movement on a bank account as the bank reports it. Reconciliation compares these against
/// what the ledger believes happened.
/// </summary>
public sealed class BankTransaction : BaseEntity
{
    public BankBranchAccount BankBranchAccount { get; private set; } = default!;
    public Guid BankBranchAccountId { get; private set; }

    public BankTransactionType Type { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DateOnly TransactionDate { get; private set; }
    public DateOnly? ValueDate { get; private set; }

    /// <summary>The reference the bank prints on the statement.</summary>
    public string? BankReference { get; private set; }
    public string? TrackingCode { get; private set; }
    public string? Description { get; private set; }

    /// <summary>The balance the bank reports after this movement.</summary>
    public Money? BalanceAfter { get; private set; }

    public Guid? DocumentId { get; private set; }
    public Guid? ChequeId { get; private set; }

    public bool IsReconciled { get; private set; }
    public Guid? ReconciliationId { get; private set; }

    private BankTransaction() { }

    public BankTransaction(BankBranchAccount bankBranchAccount, BankTransactionType type, Money amount, DateOnly transactionDate)
    {
        ArgumentNullException.ThrowIfNull(bankBranchAccount, nameof(bankBranchAccount));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A bank transaction must carry a positive amount.");
        if (amount.CurrencyCode != bankBranchAccount.CurrencyCode)
            throw new ArgumentException($"The amount must be expressed in {bankBranchAccount.CurrencyCode}.", nameof(amount));

        BankBranchAccount = bankBranchAccount;
        BankBranchAccountId = bankBranchAccount.Id;
        Type = type;
        Amount = amount;
        TransactionDate = transactionDate;
    }

    internal void SetBankReference(string? bankReference, string? trackingCode)
    {
        BankReference = string.IsNullOrWhiteSpace(bankReference) ? null : bankReference.Trim();
        TrackingCode = string.IsNullOrWhiteSpace(trackingCode) ? null : trackingCode.Trim();
    }

    internal void SetValueDate(DateOnly valueDate) => ValueDate = valueDate;

    internal void SetBalanceAfter(Money balance)
    {
        ArgumentNullException.ThrowIfNull(balance, nameof(balance));
        BalanceAfter = balance;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    internal void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    internal void LinkToCheque(Guid chequeId) => ChequeId = chequeId == Guid.Empty ? null : chequeId;

    internal void MarkAsReconciled(Guid reconciliationId)
    {
        if (IsReconciled)
            throw new InvalidOperationException("The transaction is already reconciled.");
        IsReconciled = true;
        ReconciliationId = reconciliationId;
    }

    internal void Unreconcile()
    {
        IsReconciled = false;
        ReconciliationId = null;
    }

    /// <summary>The signed effect on the account balance.</summary>
    public Money SignedAmount() => Type == BankTransactionType.Deposit ? Amount : Amount.Negate();
}
