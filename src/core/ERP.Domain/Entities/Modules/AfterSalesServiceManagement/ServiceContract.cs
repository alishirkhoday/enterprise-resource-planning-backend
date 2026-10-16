using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// A paid agreement to keep something working: what is covered, for how long, and how quickly
/// somebody has to turn up.
/// <para>
/// A warranty is an obligation that came free with a sale and expires on a date. This is the thing
/// a customer renews and is billed for, which is why it is a contract in its own right rather than
/// another date on the serial number.
/// </para>
/// </summary>
public sealed class ServiceContract : BaseEntity, IBranchScoped
{
    public string Number { get; private set; } = default!;
    public string Title { get; private set; } = default!;

    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public ServiceContractStatus Status { get; private set; }
    public ServiceCoverageType Coverage { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    public Money ContractValue { get; private set; } = default!;

    /// <summary>Hours to first response, and hours to resolution, as promised to the customer.</summary>
    public int? ResponseHours { get; private set; }
    public int? ResolutionHours { get; private set; }

    /// <summary>Visits included before the customer starts paying per call.</summary>
    public int? IncludedVisits { get; private set; }
    public int ConsumedVisits { get; private set; }

    public Guid? AssignedTeamHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    public bool AutoRenews { get; private set; }
    public int? RenewalNoticeDays { get; private set; }

    public DateOnly? SignedOn { get; private set; }
    public DateOnly? TerminatedOn { get; private set; }
    public string? TerminationReason { get; private set; }
    public string? Terms { get; private set; }

    private readonly List<ServiceContractCoverage> _coveredAssets = [];
    public IReadOnlyList<ServiceContractCoverage> CoveredAssets => _coveredAssets;

    private ServiceContract() { }

    public ServiceContract(string number, string title, Customer customer, DateRange period, ServiceCoverageType coverage, Money contractValue)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentNullException.ThrowIfNull(contractValue, nameof(contractValue));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        if (contractValue.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(contractValue), "A contract value cannot be negative.");

        Number = number.Trim();
        Title = title.Trim();
        Customer = customer;
        CustomerId = customer.Id;
        Period = period;
        Coverage = coverage;
        ContractValue = contractValue;
        CurrencyCode = contractValue.CurrencyCode;
        Status = ServiceContractStatus.Draft;
    }

    public void SetServiceLevel(int? responseHours, int? resolutionHours)
    {
        if (responseHours.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(responseHours.Value, nameof(responseHours));
        if (resolutionHours.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resolutionHours.Value, nameof(resolutionHours));
        if (responseHours.HasValue && resolutionHours.HasValue && resolutionHours.Value < responseHours.Value)
            throw new ArgumentException("Resolution cannot be promised sooner than the first response.", nameof(resolutionHours));

        ResponseHours = responseHours;
        ResolutionHours = resolutionHours;
    }

    public void SetIncludedVisits(int? includedVisits)
    {
        if (includedVisits.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(includedVisits.Value, nameof(includedVisits));
        IncludedVisits = includedVisits;
    }

    public void SetRenewal(bool autoRenews, int? renewalNoticeDays)
    {
        if (renewalNoticeDays.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(renewalNoticeDays.Value, nameof(renewalNoticeDays));
        AutoRenews = autoRenews;
        RenewalNoticeDays = renewalNoticeDays;
    }

    public void SetTerms(string? terms) => Terms = string.IsNullOrWhiteSpace(terms) ? null : terms.Trim();

    public void AssignTeam(Guid humanId) => AssignedTeamHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void Cover(ServiceContractCoverage coverage)
    {
        ArgumentNullException.ThrowIfNull(coverage, nameof(coverage));
        EnsureDraft();
        if (_coveredAssets.Any(c => c.SerialNumberId == coverage.SerialNumberId && c.ItemId == coverage.ItemId))
            throw new InvalidOperationException("That asset is already covered by this contract.");
        _coveredAssets.Add(coverage);
    }

    public void Activate(DateOnly signedOn)
    {
        EnsureDraft();
        if (_coveredAssets.Count == 0)
            throw new InvalidOperationException("A service contract that covers nothing cannot be activated.");
        SignedOn = signedOn;
        Status = ServiceContractStatus.Active;
    }

    /// <summary>Called when a service order closes against this contract.</summary>
    public void RecordVisit()
    {
        if (Status != ServiceContractStatus.Active)
            throw new InvalidOperationException($"Visits can only be recorded against an active contract; this one is {Status}.");
        ConsumedVisits++;
    }

    public void Renew(DateRange newPeriod, Money newValue)
    {
        ArgumentNullException.ThrowIfNull(newPeriod, nameof(newPeriod));
        ArgumentNullException.ThrowIfNull(newValue, nameof(newValue));
        if (Status != ServiceContractStatus.Active)
            throw new InvalidOperationException("Only an active contract can be renewed.");
        if (newValue.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"The renewal value must be expressed in {CurrencyCode}.", nameof(newValue));

        Period = newPeriod;
        ContractValue = newValue;
        ConsumedVisits = 0;
    }

    public void Terminate(DateOnly terminatedOn, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != ServiceContractStatus.Active)
            throw new InvalidOperationException("Only an active contract can be terminated.");
        TerminatedOn = terminatedOn;
        TerminationReason = reason.Trim();
        Status = ServiceContractStatus.Terminated;
    }

    public void Expire() => Status = ServiceContractStatus.Expired;

    public bool IsEffectiveOn(DateOnly date) => Status == ServiceContractStatus.Active && Period.Contains(date);

    /// <summary>Whether this contract still owes the customer a visit without extra charge.</summary>
    public bool HasVisitsRemaining => IncludedVisits is null || ConsumedVisits < IncludedVisits.Value;

    public bool Covers(Guid itemId, Guid? serialNumberId, DateOnly onDate) =>
        IsEffectiveOn(onDate)
        && _coveredAssets.Any(c => c.ItemId == itemId && (c.SerialNumberId is null || c.SerialNumberId == serialNumberId));

    public bool IsDueForRenewal(DateOnly asOf) =>
        Status == ServiceContractStatus.Active
        && AutoRenews
        && Period.EndDate.HasValue
        && RenewalNoticeDays.HasValue
        && Period.EndDate.Value.DayNumber - asOf.DayNumber <= RenewalNoticeDays.Value;

    private void EnsureDraft()
    {
        if (Status != ServiceContractStatus.Draft)
            throw new InvalidOperationException($"A contract in state {Status} can no longer be edited.");
    }
}
