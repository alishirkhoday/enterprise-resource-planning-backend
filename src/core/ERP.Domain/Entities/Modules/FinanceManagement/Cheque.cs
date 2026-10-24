namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A cheque the organization holds or has issued. Its lifecycle is a chain of states rather than
/// a single flag, because each transition is itself an accounting event.
/// </summary>
public sealed partial class Cheque : AggregateRoot, IBranchScoped
{
    public ChequeType Type { get; private set; }
    public ChequeStatus Status { get; private set; }

    /// <summary>The number printed on the leaf.</summary>
    public string Number { get; private set; } = default!;

    /// <summary>The serial identifier assigned by the national cheque registry.</summary>
    public string? SayadNumber { get; private set; }

    public Money Amount { get; private set; } = default!;
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }

    /// <summary>The account the cheque is drawn on, for a cheque the organization issued.</summary>
    public BankBranchAccount? DrawnOnAccount { get; private set; }
    public Guid? DrawnOnAccountId { get; private set; }

    /// <summary>The bank of the party that issued a cheque the organization received.</summary>
    public string? DrawerBankName { get; private set; }
    public string? DrawerName { get; private set; }
    public string? DrawerAccountNumber { get; private set; }

    /// <summary>Who the cheque is payable to, for a cheque the organization issued.</summary>
    public string? PayeeName { get; private set; }

    /// <summary>The subsidiary account the cheque belongs to, such as the customer who handed it over.</summary>
    public Guid? DetailAccountId { get; private set; }

    public Guid? ChequeBookId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>The account the cheque was deposited into, once it is in collection.</summary>
    public Guid? DepositedToAccountId { get; private set; }
    public DateOnly? DepositedOn { get; private set; }
    public DateOnly? SettledOn { get; private set; }

    public string? BounceReason { get; private set; }
    public string? Description { get; private set; }

    private readonly List<ChequeOperation> _operations = [];
    public IReadOnlyList<ChequeOperation> Operations => _operations.AsReadOnly();

    private Cheque() { }

    public Cheque(ChequeType type, string number, Money amount, DateOnly issueDate, DateOnly dueDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A cheque must carry a positive amount.");
        if (dueDate < issueDate)
            throw new ArgumentOutOfRangeException(nameof(dueDate), "DueDate cannot be earlier than IssueDate.");

        Type = type;
        Number = number.Trim();
        Amount = amount;
        IssueDate = issueDate;
        DueDate = dueDate;
        Status = ChequeStatus.InHand;
        Record(ChequeOperationType.Registered, issueDate, null);
    }

    public void SetSayadNumber(string sayadNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sayadNumber, nameof(sayadNumber));
        SayadNumber = sayadNumber.Trim();
    }

    public void SetDrawer(string? bankName, string? drawerName, string? accountNumber)
    {
        if (Type != ChequeType.Received)
            throw new InvalidOperationException("Drawer details only apply to a received cheque.");
        DrawerBankName = string.IsNullOrWhiteSpace(bankName) ? null : bankName.Trim();
        DrawerName = string.IsNullOrWhiteSpace(drawerName) ? null : drawerName.Trim();
        DrawerAccountNumber = string.IsNullOrWhiteSpace(accountNumber) ? null : accountNumber.Trim();
    }

    public void SetDrawnOnAccount(BankBranchAccount account, Guid? chequeBookId = null)
    {
        ArgumentNullException.ThrowIfNull(account, nameof(account));
        if (Type != ChequeType.Issued)
            throw new InvalidOperationException("A drawn on account only applies to an issued cheque.");
        DrawnOnAccount = account;
        DrawnOnAccountId = account.Id;
        ChequeBookId = chequeBookId == Guid.Empty ? null : chequeBookId;
    }

    public void SetPayee(string payeeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payeeName, nameof(payeeName));
        PayeeName = payeeName.Trim();
    }

    public void LinkToDetailAccount(Guid detailAccountId) => DetailAccountId = detailAccountId == Guid.Empty ? null : detailAccountId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Hands a received cheque to the bank for collection.</summary>
    public void Deposit(Guid bankAccountId, DateOnly depositedOn)
    {
        if (Type != ChequeType.Received)
            throw new InvalidOperationException("Only a received cheque can be deposited.");
        if (Status != ChequeStatus.InHand)
            throw new InvalidOperationException($"A cheque in state {Status} cannot be deposited.");

        DepositedToAccountId = bankAccountId;
        DepositedOn = depositedOn;
        Status = ChequeStatus.InCollection;
        Record(ChequeOperationType.Deposited, depositedOn, null);
    }

    /// <summary>Marks the cheque as honoured by the bank.</summary>
    public void Clear(DateOnly settledOn)
    {
        if (Status is not (ChequeStatus.InCollection or ChequeStatus.Issued))
            throw new InvalidOperationException($"A cheque in state {Status} cannot be cleared.");
        Status = ChequeStatus.Cleared;
        SettledOn = settledOn;
        Record(ChequeOperationType.Cleared, settledOn, null);
    }

    public void Bounce(DateOnly bouncedOn, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status is not (ChequeStatus.InCollection or ChequeStatus.Issued))
            throw new InvalidOperationException($"A cheque in state {Status} cannot bounce.");
        Status = ChequeStatus.Bounced;
        BounceReason = reason.Trim();
        Record(ChequeOperationType.Bounced, bouncedOn, reason);
        Raise(new ChequeBounced(Id, Number, BounceReason));
    }

    /// <summary>Passes a received cheque on to a third party in settlement of a debt.</summary>
    public void Endorse(DateOnly endorsedOn, string endorsedTo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endorsedTo, nameof(endorsedTo));
        if (Type != ChequeType.Received)
            throw new InvalidOperationException("Only a received cheque can be endorsed.");
        if (Status != ChequeStatus.InHand)
            throw new InvalidOperationException($"A cheque in state {Status} cannot be endorsed.");
        Status = ChequeStatus.Endorsed;
        Record(ChequeOperationType.Endorsed, endorsedOn, endorsedTo);
    }

    /// <summary>Marks an issued cheque as handed to its payee.</summary>
    public void Issue(DateOnly issuedOn)
    {
        if (Type != ChequeType.Issued)
            throw new InvalidOperationException("Only an issued cheque can be handed over.");
        if (Status != ChequeStatus.InHand)
            throw new InvalidOperationException($"A cheque in state {Status} cannot be handed over.");
        Status = ChequeStatus.Issued;
        Record(ChequeOperationType.Issued, issuedOn, null);
    }

    /// <summary>Takes a bounced received cheque back from the bank.</summary>
    public void ReturnToHolder(DateOnly returnedOn)
    {
        if (Status != ChequeStatus.Bounced)
            throw new InvalidOperationException("Only a bounced cheque can be returned.");
        Status = ChequeStatus.Returned;
        Record(ChequeOperationType.Returned, returnedOn, null);
    }

    public void Cancel(DateOnly cancelledOn, string? reason = null)
    {
        if (Status is ChequeStatus.Cleared or ChequeStatus.Endorsed)
            throw new InvalidOperationException($"A cheque in state {Status} cannot be cancelled.");
        Status = ChequeStatus.Cancelled;
        Record(ChequeOperationType.Cancelled, cancelledOn, reason);
    }

    public bool IsOverdue(DateOnly asOf) =>
        Status is ChequeStatus.InHand or ChequeStatus.InCollection or ChequeStatus.Issued && DueDate < asOf;

    private void Record(ChequeOperationType type, DateOnly occurredOn, string? description) =>
        _operations.Add(new ChequeOperation(this, type, occurredOn, Status, description));
}
