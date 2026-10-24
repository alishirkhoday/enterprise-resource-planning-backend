namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A named bonus that can be granted on a pay slip, either as a set sum or as a share of pay.
/// </summary>
public sealed class Reward : AggregateRoot
{
    public string Title { get; private set; } = default!;
    public SalaryItemCalculation Calculation { get; private set; }

    public Money? Amount { get; private set; }
    public Percentage? Rate { get; private set; }

    public bool IsTaxable { get; private set; }
    public bool IsInsurable { get; private set; }
    public Guid? AccountId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<PayrollReward> _bonuses = [];
    public IReadOnlyList<PayrollReward> Bonuses => _bonuses.AsReadOnly();

    private Reward() { }

    public Reward(string title, Money amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A reward must be a positive amount.");
        Title = title.Trim();
        Amount = amount;
        Calculation = SalaryItemCalculation.FixedAmount;
        IsTaxable = true;
        IsActive = true;
    }

    public Reward(string title, Percentage rate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        Title = title.Trim();
        Rate = rate;
        Calculation = SalaryItemCalculation.PercentageOfBase;
        IsTaxable = true;
        IsActive = true;
    }

    public void Update(string title, Money amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A reward must be a positive amount.");
        Title = title.Trim();
        Amount = amount;
        Rate = null;
        Calculation = SalaryItemCalculation.FixedAmount;
    }

    public void SetTaxTreatment(bool isTaxable, bool isInsurable)
    {
        IsTaxable = isTaxable;
        IsInsurable = isInsurable;
    }

    public void LinkAccount(Guid accountId) => AccountId = accountId == Guid.Empty ? null : accountId;

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();

    /// <summary>The amount this reward yields against a base pay.</summary>
    public Money CalculateOn(Money baseAmount)
    {
        ArgumentNullException.ThrowIfNull(baseAmount, nameof(baseAmount));
        return Calculation == SalaryItemCalculation.FixedAmount
            ? Amount ?? Money.Zero(baseAmount.CurrencyCode)
            : Rate is null ? Money.Zero(baseAmount.CurrencyCode) : baseAmount.Percent(Rate);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
