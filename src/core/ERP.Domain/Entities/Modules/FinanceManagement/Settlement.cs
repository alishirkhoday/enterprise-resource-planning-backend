namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A receipt or a payment, and how it is spread across the open items it clears. Keeping the
/// allocation explicit is what makes a partly paid invoice traceable to the money that paid it.
/// </summary>
public sealed partial class Settlement : AggregateRoot, IBranchScoped
{
    public string? Number { get; private set; }
    public SettlementDirection Direction { get; private set; }
    public SettlementMethod Method { get; private set; }
    public SettlementStatus Status { get; private set; }

    /// <summary>The subsidiary account of the party the money came from or went to.</summary>
    public Guid PartyDetailAccountId { get; private set; }

    public Money Amount { get; private set; } = default!;
    public DateOnly SettlementDate { get; private set; }

    /// <summary>Where the money landed or came from, depending on the method.</summary>
    public Guid? CashBoxId { get; private set; }
    public Guid? BankBranchAccountId { get; private set; }
    public Guid? ChequeId { get; private set; }

    public Guid? DocumentId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? TrackingCode { get; private set; }
    public string? Description { get; private set; }

    private readonly List<SettlementAllocation> _allocations = [];
    public IReadOnlyList<SettlementAllocation> Allocations => _allocations.AsReadOnly();

    private Settlement() { }

    public Settlement(SettlementDirection direction, SettlementMethod method, Guid partyDetailAccountId, Money amount, DateOnly settlementDate)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (partyDetailAccountId == Guid.Empty)
            throw new ArgumentException("PartyDetailAccountId is empty.", nameof(partyDetailAccountId));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A settlement must carry a positive amount.");

        Direction = direction;
        Method = method;
        PartyDetailAccountId = partyDetailAccountId;
        Amount = amount;
        SettlementDate = settlementDate;
        Status = SettlementStatus.Draft;
    }

    public void SetNumber(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        Number = number.Trim();
    }

    public void SetCashBox(Guid cashBoxId)
    {
        EnsureDraft();
        CashBoxId = cashBoxId == Guid.Empty ? null : cashBoxId;
    }

    public void SetBankAccount(Guid bankBranchAccountId)
    {
        EnsureDraft();
        BankBranchAccountId = bankBranchAccountId == Guid.Empty ? null : bankBranchAccountId;
    }

    public void SetCheque(Guid chequeId)
    {
        EnsureDraft();
        ChequeId = chequeId == Guid.Empty ? null : chequeId;
    }

    public void SetTrackingCode(string? trackingCode) => TrackingCode = string.IsNullOrWhiteSpace(trackingCode) ? null : trackingCode.Trim();

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    public void Allocate(SettlementAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        EnsureCanAddChild(allocation, allocation.SettlementId, _allocations);

        EnsureDraft();
        if (allocation.Amount.CurrencyCode != Amount.CurrencyCode)
            throw new InvalidOperationException($"The allocation must be expressed in {Amount.CurrencyCode}.");
        if (allocation.Amount.Amount > UnallocatedAmount().Amount)
            throw new InvalidOperationException($"Only {UnallocatedAmount()} is left to allocate.");
        _allocations.Add(allocation);
    }

    public void RemoveAllocation(SettlementAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        if (allocation.SettlementId != Id || !_allocations.Contains(allocation))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(allocation, nameof(allocation));
        EnsureDraft();
        _allocations.Remove(allocation);
    }

    public Money AllocatedAmount() =>
        _allocations.Count == 0
            ? Money.Zero(Amount.CurrencyCode)
            : _allocations.Select(a => a.Amount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money UnallocatedAmount() => Amount.Subtract(AllocatedAmount());

    /// <summary>
    /// Confirms the settlement. Leftover money is allowed and becomes an advance held on the
    /// party account, which is why a full allocation is not demanded here.
    /// </summary>
    public void Confirm()
    {
        EnsureDraft();
        if (_allocations.Count == 0 && Method != SettlementMethod.Advance)
            throw new InvalidOperationException("A settlement must either be allocated or recorded as an advance.");
        Status = SettlementStatus.Confirmed;
        Raise(new SettlementConfirmed(Id, PartyDetailAccountId, Amount.Amount, Amount.CurrencyCode));
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == SettlementStatus.Cancelled)
            return;
        Status = SettlementStatus.Cancelled;
        Description = reason.Trim();
    }

    private void EnsureDraft()
    {
        if (Status != SettlementStatus.Draft)
            throw new InvalidOperationException($"The settlement is {Status} and can no longer be edited.");
    }
}
