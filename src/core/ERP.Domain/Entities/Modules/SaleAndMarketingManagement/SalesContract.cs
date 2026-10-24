using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// A standing agreement with a customer: agreed prices and terms for a period, which orders
/// raised under it inherit.
/// </summary>
public sealed class SalesContract : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;
    public string Title { get; private set; } = default!;

    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public SalesContractStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    /// <summary>The value the customer has committed to over the term, when one was agreed.</summary>
    public Money? CommittedValue { get; private set; }

    /// <summary>How much has actually been ordered against it.</summary>
    public Money ConsumedValue { get; private set; } = default!;

    public Guid? PriceListId { get; private set; }
    public Guid? PaymentTermId { get; private set; }
    public Guid? SalesRepHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    public bool AutoRenews { get; private set; }
    public int? RenewalNoticeDays { get; private set; }

    public DateOnly? SignedOn { get; private set; }
    public DateOnly? TerminatedOn { get; private set; }
    public string? TerminationReason { get; private set; }
    public string? Terms { get; private set; }

    private SalesContract() { }

    public SalesContract(string number, string title, Customer customer, DateRange period, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Number = number.Trim();
        Title = title.Trim();
        Customer = customer;
        CustomerId = customer.Id;
        Period = period;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ConsumedValue = Money.Zero(CurrencyCode);
        Status = SalesContractStatus.Draft;
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

    public void SetCommercialTerms(Guid? priceListId, Guid? paymentTermId, string? terms)
    {
        PriceListId = priceListId == Guid.Empty ? null : priceListId;
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

    public void AssignSalesRep(Guid humanId) => SalesRepHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void Activate(DateOnly signedOn)
    {
        if (Status != SalesContractStatus.Draft)
            throw new InvalidOperationException($"Only a draft contract can be activated; this one is {Status}.");
        SignedOn = signedOn;
        Status = SalesContractStatus.Active;
    }

    public void RecordConsumption(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (Status != SalesContractStatus.Active)
            throw new InvalidOperationException($"Orders can only be recorded against an active contract; this one is {Status}.");
        ConsumedValue = ConsumedValue.Add(amount);
    }

    public void Renew(DateRange newPeriod)
    {
        ArgumentNullException.ThrowIfNull(newPeriod, nameof(newPeriod));
        if (Status != SalesContractStatus.Active)
            throw new InvalidOperationException("Only an active contract can be renewed.");
        Period = newPeriod;
        ConsumedValue = Money.Zero(CurrencyCode);
    }

    public void Terminate(DateOnly terminatedOn, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != SalesContractStatus.Active)
            throw new InvalidOperationException("Only an active contract can be terminated.");
        TerminatedOn = terminatedOn;
        TerminationReason = reason.Trim();
        Status = SalesContractStatus.Terminated;
    }

    public void Expire() => Status = SalesContractStatus.Expired;

    public Money? RemainingCommitment => CommittedValue?.Subtract(ConsumedValue);

    public bool IsEffectiveOn(DateOnly date) => Status == SalesContractStatus.Active && Period.Contains(date);

    public bool IsDueForRenewal(DateOnly asOf) =>
        Status == SalesContractStatus.Active
        && AutoRenews
        && Period.EndDate.HasValue
        && RenewalNoticeDays.HasValue
        && Period.EndDate.Value.DayNumber - asOf.DayNumber <= RenewalNoticeDays.Value;
}
