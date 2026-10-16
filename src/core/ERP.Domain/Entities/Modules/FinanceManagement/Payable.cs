namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// An amount the organization owes a supplier or other party, tracked until it is fully settled.
/// </summary>
public sealed class Payable : BaseEntity, IBranchScoped
{
    /// <summary>The subsidiary account of the party that is owed the money.</summary>
    public Guid PartyDetailAccountId { get; private set; }

    public string? DocumentNumber { get; private set; }
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public Guid? DocumentId { get; private set; }
    public Guid? PaymentTermId { get; private set; }
    public Guid? BranchId { get; private set; }

    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }

    public Money OriginalAmount { get; private set; } = default!;
    public Money SettledAmount { get; private set; } = default!;
    public Money OutstandingAmount => OriginalAmount.Subtract(SettledAmount);

    public PayableStatus Status { get; private set; }
    public DateOnly? SettledOn { get; private set; }

    /// <summary>Holds the payable back from payment runs while a dispute is open.</summary>
    public bool IsOnHold { get; private set; }
    public string? HoldReason { get; private set; }
    public string? Description { get; private set; }

    private Payable() { }

    public Payable(Guid partyDetailAccountId, Money originalAmount, DateOnly issueDate, DateOnly dueDate)
    {
        ArgumentNullException.ThrowIfNull(originalAmount, nameof(originalAmount));
        if (partyDetailAccountId == Guid.Empty)
            throw new ArgumentException("PartyDetailAccountId is empty.", nameof(partyDetailAccountId));
        if (!originalAmount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(originalAmount), "A payable must be a positive amount.");
        if (dueDate < issueDate)
            throw new ArgumentOutOfRangeException(nameof(dueDate), "DueDate cannot be earlier than IssueDate.");

        PartyDetailAccountId = partyDetailAccountId;
        OriginalAmount = originalAmount;
        SettledAmount = Money.Zero(originalAmount.CurrencyCode);
        IssueDate = issueDate;
        DueDate = dueDate;
        Status = PayableStatus.Open;
    }

    public void SetSource(string referenceType, string referenceId, string? documentNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
        DocumentNumber = string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber.Trim();
    }

    public void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    public void ApplyPaymentTerm(Guid paymentTermId, DateOnly dueDate)
    {
        PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
        if (dueDate < IssueDate)
            throw new ArgumentOutOfRangeException(nameof(dueDate), "DueDate cannot be earlier than IssueDate.");
        DueDate = dueDate;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Settle(Money amount, DateOnly settledOn)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (IsOnHold)
            throw new InvalidOperationException($"The payable is on hold: {HoldReason}");
        if (Status == PayableStatus.Cancelled)
            throw new InvalidOperationException("A cancelled payable cannot be settled.");
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A settlement must be a positive amount.");
        if (amount.Amount > OutstandingAmount.Amount)
            throw new InvalidOperationException($"The settlement of {amount} exceeds the outstanding {OutstandingAmount}.");

        SettledAmount = SettledAmount.Add(amount);
        if (OutstandingAmount.IsZero)
        {
            Status = PayableStatus.Settled;
            SettledOn = settledOn;
        }
        else
        {
            Status = PayableStatus.PartiallySettled;
        }
    }

    public void ReverseSettlement(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (amount.Amount > SettledAmount.Amount)
            throw new InvalidOperationException("Cannot reverse more than has been settled.");
        SettledAmount = SettledAmount.Subtract(amount);
        SettledOn = null;
        Status = SettledAmount.IsZero ? PayableStatus.Open : PayableStatus.PartiallySettled;
    }

    public void PutOnHold(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        IsOnHold = true;
        HoldReason = reason.Trim();
    }

    public void ReleaseHold()
    {
        IsOnHold = false;
        HoldReason = null;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (!SettledAmount.IsZero)
            throw new InvalidOperationException("A partly settled payable cannot be cancelled.");
        Status = PayableStatus.Cancelled;
        Description = reason.Trim();
    }

    public bool IsOverdueOn(DateOnly date) =>
        Status is PayableStatus.Open or PayableStatus.PartiallySettled && date > DueDate;

    public int DaysOverdueOn(DateOnly date) => IsOverdueOn(date) ? date.DayNumber - DueDate.DayNumber : 0;
}
