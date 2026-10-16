namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A standing agreement with a supplier: agreed prices and terms for a period, which purchase
/// orders raised under it inherit. The buying-side counterpart of a sales contract.
/// <para>
/// Its real job is the commitment. A framework agreement usually promises the supplier a volume in
/// exchange for the price, so what has been consumed against it has to be tracked as orders are
/// placed - otherwise nobody notices the commitment was missed until the supplier asks for it.
/// </para>
/// </summary>
public sealed class PurchaseContract : BaseEntity, IBranchScoped
{
    public string Number { get; private set; } = default!;
    public string Title { get; private set; } = default!;

    public Supplier Supplier { get; private set; } = default!;
    public Guid SupplierId { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public PurchaseContractStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    /// <summary>The value this organization committed to buy over the term, when one was agreed.</summary>
    public Money? CommittedValue { get; private set; }

    /// <summary>How much has actually been ordered against it.</summary>
    public Money ConsumedValue { get; private set; } = default!;

    public Guid? PaymentTermId { get; private set; }
    public Guid? BuyerHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    public bool AutoRenews { get; private set; }
    public int? RenewalNoticeDays { get; private set; }

    public DateOnly? SignedOn { get; private set; }
    public DateOnly? TerminatedOn { get; private set; }
    public string? TerminationReason { get; private set; }
    public string? Terms { get; private set; }

    private readonly List<PurchaseContractItem> _items = [];
    public IReadOnlyList<PurchaseContractItem> Items => _items;

    private PurchaseContract() { }

    public PurchaseContract(string number, string title, Supplier supplier, DateRange period, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Number = number.Trim();
        Title = title.Trim();
        Supplier = supplier;
        SupplierId = supplier.Id;
        Period = period;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ConsumedValue = Money.Zero(CurrencyCode);
        Status = PurchaseContractStatus.Draft;
    }

    public void SetCommittedValue(Money committedValue)
    {
        ArgumentNullException.ThrowIfNull(committedValue, nameof(committedValue));
        if (committedValue.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"The committed value must be expressed in {CurrencyCode}.", nameof(committedValue));
        if (committedValue.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(committedValue), "A committed value cannot be negative.");
        CommittedValue = committedValue;
    }

    public void SetCommercialTerms(Guid? paymentTermId, string? terms)
    {
        PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
        Terms = string.IsNullOrWhiteSpace(terms) ? null : terms.Trim();
    }

    public void SetRenewal(bool autoRenews, int? renewalNoticeDays)
    {
        if (renewalNoticeDays.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(renewalNoticeDays.Value, nameof(renewalNoticeDays));
        AutoRenews = autoRenews;
        RenewalNoticeDays = renewalNoticeDays;
    }

    public void AssignBuyer(Guid humanId) => BuyerHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddItem(PurchaseContractItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        if (item.AgreedUnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Prices on this contract must be expressed in {CurrencyCode}.");
        if (_items.Any(i => i.ItemId == item.ItemId))
            throw new InvalidOperationException("This item already has an agreed price on this contract.");
        _items.Add(item);
    }

    public void RemoveItem(PurchaseContractItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Remove(item);
    }

    public void Activate(DateOnly signedOn)
    {
        EnsureDraft();
        SignedOn = signedOn;
        Status = PurchaseContractStatus.Active;
    }

    public void RecordConsumption(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (Status != PurchaseContractStatus.Active)
            throw new InvalidOperationException($"Orders can only be recorded against an active contract; this one is {Status}.");
        ConsumedValue = ConsumedValue.Add(amount);
    }

    public void Renew(DateRange newPeriod)
    {
        ArgumentNullException.ThrowIfNull(newPeriod, nameof(newPeriod));
        if (Status != PurchaseContractStatus.Active)
            throw new InvalidOperationException("Only an active contract can be renewed.");
        Period = newPeriod;
        ConsumedValue = Money.Zero(CurrencyCode);
        foreach (var item in _items)
        {
            item.ResetConsumption();
        }
    }

    public void Terminate(DateOnly terminatedOn, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != PurchaseContractStatus.Active)
            throw new InvalidOperationException("Only an active contract can be terminated.");
        TerminatedOn = terminatedOn;
        TerminationReason = reason.Trim();
        Status = PurchaseContractStatus.Terminated;
    }

    public void Expire() => Status = PurchaseContractStatus.Expired;

    /// <summary>The agreed price for an item on a given date, if this contract covers it.</summary>
    public Money? PriceFor(Guid itemId, DateOnly onDate) =>
        IsEffectiveOn(onDate) ? _items.FirstOrDefault(i => i.ItemId == itemId)?.AgreedUnitPrice : null;

    public Money? RemainingCommitment => CommittedValue?.Subtract(ConsumedValue);

    public bool IsEffectiveOn(DateOnly date) => Status == PurchaseContractStatus.Active && Period.Contains(date);

    public bool IsDueForRenewal(DateOnly asOf) =>
        Status == PurchaseContractStatus.Active
        && AutoRenews
        && Period.EndDate.HasValue
        && RenewalNoticeDays.HasValue
        && Period.EndDate.Value.DayNumber - asOf.DayNumber <= RenewalNoticeDays.Value;

    private void EnsureDraft()
    {
        if (Status != PurchaseContractStatus.Draft)
            throw new InvalidOperationException($"A contract in state {Status} can no longer be edited.");
    }
}
