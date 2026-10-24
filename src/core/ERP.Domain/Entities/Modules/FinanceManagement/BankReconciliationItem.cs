namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// One thing that explains part of the gap between the bank statement and the ledger, such as a
/// cheque written but not yet presented.
/// </summary>
public sealed class BankReconciliationItem : BaseEntity
{
    public BankReconciliation Reconciliation { get; private set; } = default!;
    public Guid ReconciliationId { get; private set; }

    public BankReconciliationItemType Type { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DateOnly ItemDate { get; private set; }
    public string Description { get; private set; } = default!;

    public Guid? BankTransactionId { get; private set; }
    public Guid? DocumentId { get; private set; }
    public Guid? ChequeId { get; private set; }

    private BankReconciliationItem() { }

    public BankReconciliationItem(BankReconciliation reconciliation, BankReconciliationItemType type, Money amount, DateOnly itemDate, string description)
    {
        ArgumentNullException.ThrowIfNull(reconciliation, nameof(reconciliation));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A reconciling item must carry a positive amount.");

        Reconciliation = reconciliation;
        ReconciliationId = reconciliation.Id;
        Type = type;
        Amount = amount;
        ItemDate = itemDate;
        Description = description.Trim();
    }

    internal void LinkToBankTransaction(Guid bankTransactionId) => BankTransactionId = bankTransactionId == Guid.Empty ? null : bankTransactionId;

    internal void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    internal void LinkToCheque(Guid chequeId) => ChequeId = chequeId == Guid.Empty ? null : chequeId;

    /// <summary>
    /// How this item moves the book balance towards the statement balance. Items the bank has not
    /// seen yet reduce it; items the ledger has not seen yet increase it.
    /// </summary>
    public Money SignedAmount() => Type switch
    {
        BankReconciliationItemType.OutstandingCheque => Amount.Negate(),
        BankReconciliationItemType.DepositInTransit => Amount,
        BankReconciliationItemType.BankCharge => Amount.Negate(),
        BankReconciliationItemType.BankInterest => Amount,
        BankReconciliationItemType.DirectDebit => Amount.Negate(),
        BankReconciliationItemType.DirectCredit => Amount,
        BankReconciliationItemType.BookError => Amount,
        BankReconciliationItemType.BankError => Amount.Negate(),
        _ => Amount
    };
}
