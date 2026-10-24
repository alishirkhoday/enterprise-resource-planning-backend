namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// The social insurance scheme an employer contributes to, and the split between the employee,
/// the employer and any unemployment levy.
/// </summary>
public sealed class InsurancePolicy : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public InsuranceType Type { get; private set; }

    /// <summary>The insurer branch the employer is registered with.</summary>
    public string? InsurerBranchCode { get; private set; }

    /// <summary>The employer workshop identifier the insurer knows it by.</summary>
    public string? WorkshopCode { get; private set; }

    public Percentage EmployeeRate { get; private set; } = default!;
    public Percentage EmployerRate { get; private set; } = default!;
    public Percentage UnemploymentRate { get; private set; } = default!;

    /// <summary>The floor the insurable base is raised to, when one is imposed.</summary>
    public Money? MinimumInsurableBase { get; private set; }

    /// <summary>The ceiling the insurable base is capped at, when one is imposed.</summary>
    public Money? MaximumInsurableBase { get; private set; }

    public DateRange EffectivePeriod { get; private set; } = default!;
    public Guid? PayableAccountId { get; private set; }
    public bool IsActive { get; private set; }

    private InsurancePolicy() { }

    public InsurancePolicy(string code, string name, InsuranceType type, Percentage employeeRate, Percentage employerRate, DateRange effectivePeriod)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(employeeRate, nameof(employeeRate));
        ArgumentNullException.ThrowIfNull(employerRate, nameof(employerRate));
        ArgumentNullException.ThrowIfNull(effectivePeriod, nameof(effectivePeriod));

        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        EmployeeRate = employeeRate;
        EmployerRate = employerRate;
        UnemploymentRate = Percentage.Zero();
        EffectivePeriod = effectivePeriod;
        IsActive = true;
    }

    public void SetUnemploymentRate(Percentage rate)
    {
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        UnemploymentRate = rate;
    }

    public void SetRegistration(string? insurerBranchCode, string? workshopCode)
    {
        InsurerBranchCode = string.IsNullOrWhiteSpace(insurerBranchCode) ? null : insurerBranchCode.Trim();
        WorkshopCode = string.IsNullOrWhiteSpace(workshopCode) ? null : workshopCode.Trim();
    }

    public void SetInsurableBounds(Money? minimum, Money? maximum)
    {
        if (minimum is not null && maximum is not null && maximum.Amount < minimum.Amount)
            throw new ArgumentOutOfRangeException(nameof(maximum), "The ceiling cannot be below the floor.");
        MinimumInsurableBase = minimum;
        MaximumInsurableBase = maximum;
    }

    public void LinkPayableAccount(Guid accountId) => PayableAccountId = accountId == Guid.Empty ? null : accountId;

    /// <summary>Applies the floor and ceiling to the raw insurable pay.</summary>
    public Money BoundedBase(Money insurableBase)
    {
        ArgumentNullException.ThrowIfNull(insurableBase, nameof(insurableBase));
        var amount = insurableBase.Amount;
        if (MinimumInsurableBase is not null && amount < MinimumInsurableBase.Amount)
            amount = MinimumInsurableBase.Amount;
        if (MaximumInsurableBase is not null && amount > MaximumInsurableBase.Amount)
            amount = MaximumInsurableBase.Amount;
        return Money.Create(amount, insurableBase.CurrencyCode);
    }

    public Money EmployeeShare(Money insurableBase) => BoundedBase(insurableBase).Percent(EmployeeRate);

    public Money EmployerShare(Money insurableBase) => BoundedBase(insurableBase).Percent(EmployerRate);

    public Money UnemploymentShare(Money insurableBase) => BoundedBase(insurableBase).Percent(UnemploymentRate);

    public Money TotalContribution(Money insurableBase) =>
        EmployeeShare(insurableBase).Add(EmployerShare(insurableBase)).Add(UnemploymentShare(insurableBase));

    public bool IsEffectiveOn(DateOnly date) => IsActive && EffectivePeriod.Contains(date);

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
