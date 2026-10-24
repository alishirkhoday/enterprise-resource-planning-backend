namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// An amount a customer owes, tracked from the moment it is raised until it is fully settled.
/// Ageing reports read these rather than re-deriving them from the journal.
/// </summary>
public sealed class Receivable : AggregateRoot, IBranchScoped
{
    /// <summary>The subsidiary account of the party that owes the money.</summary>
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

    public ReceivableStatus Status { get; private set; }
    public DateOnly? SettledOn { get; private set; }
    public string? Description { get; private set; }

    private Receivable() { }

    public Receivable(Guid partyDetailAccountId, Money originalAmount, DateOnly issueDate, DateOnly dueDate)
    {
        ArgumentNullException.ThrowIfNull(originalAmount, nameof(originalAmount));
        if (partyDetailAccountId == Guid.Empty)
            throw new ArgumentException("PartyDetailAccountId is empty.", nameof(partyDetailAccountId));
        if (!originalAmount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(originalAmount), "A receivable must be a positive amount.");
        if (dueDate < issueDate)
            throw new ArgumentOutOfRangeException(nameof(dueDate), "DueDate cannot be earlier than IssueDate.");

        PartyDetailAccountId = partyDetailAccountId;
        OriginalAmount = originalAmount;
        SettledAmount = Money.Zero(originalAmount.CurrencyCode);
        IssueDate = issueDate;
        DueDate = dueDate;
        Status = ReceivableStatus.Open;
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
        if (dueDate < IssueDate)
            throw new ArgumentOutOfRangeException(nameof(dueDate), "DueDate cannot be earlier than IssueDate.");
        PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
        DueDate = dueDate;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Applies a payment, writing off the debt once nothing is left outstanding.</summary>
    public void Settle(Money amount, DateOnly settledOn)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (Status == ReceivableStatus.WrittenOff)
            throw new InvalidOperationException("A written off receivable cannot be settled.");
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A settlement must be a positive amount.");
        if (amount.Amount > OutstandingAmount.Amount)
            throw new InvalidOperationException($"The settlement of {amount} exceeds the outstanding {OutstandingAmount}.");

        SettledAmount = SettledAmount.Add(amount);
        if (OutstandingAmount.IsZero)
        {
            Status = ReceivableStatus.Settled;
            SettledOn = settledOn;
        }
        else
        {
            Status = ReceivableStatus.PartiallySettled;
        }
    }

    /// <summary>Reverses a settlement, for instance when the cheque that paid it bounced.</summary>
    public void ReverseSettlement(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (amount.Amount > SettledAmount.Amount)
            throw new InvalidOperationException("Cannot reverse more than has been settled.");
        SettledAmount = SettledAmount.Subtract(amount);
        SettledOn = null;
        Status = SettledAmount.IsZero ? ReceivableStatus.Open : ReceivableStatus.PartiallySettled;
    }

    public void WriteOff(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == ReceivableStatus.Settled)
            throw new InvalidOperationException("A settled receivable cannot be written off.");
        Status = ReceivableStatus.WrittenOff;
        Description = reason.Trim();
    }

    public bool IsOverdueOn(DateOnly date) =>
        Status is ReceivableStatus.Open or ReceivableStatus.PartiallySettled && date > DueDate;

    public int DaysOverdueOn(DateOnly date) => IsOverdueOn(date) ? date.DayNumber - DueDate.DayNumber : 0;
}
