namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A named withholding that can be applied to a pay slip, either as a set sum or as a share of pay.
/// </summary>
public sealed class Deduction : BaseEntity
{
    public string Title { get; private set; } = default!;
    public SalaryItemCalculation Calculation { get; private set; }

    public Money? Amount { get; private set; }
    public Percentage? Rate { get; private set; }

    /// <summary>Whether the deduction reduces the income the employee is taxed on.</summary>
    public bool ReducesTaxableBase { get; private set; }

    public Guid? AccountId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<PayrollDeduction> _deductions = [];
    public IReadOnlyList<PayrollDeduction> Deductions => _deductions;

    private Deduction() { }

    public Deduction(string title, Money amount)
    {
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A deduction must be a positive amount.");
        Title = title.Trim();
        Amount = amount;
        Calculation = SalaryItemCalculation.FixedAmount;
        IsActive = true;
    }

    public Deduction(string title, Percentage rate)
    {
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        Title = title.Trim();
        Rate = rate;
        Calculation = SalaryItemCalculation.PercentageOfBase;
        IsActive = true;
    }

    public void Update(string title, Money amount)
    {
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A deduction must be a positive amount.");
        Title = title.Trim();
        Amount = amount;
        Rate = null;
        Calculation = SalaryItemCalculation.FixedAmount;
    }

    public void MarkAsReducingTaxableBase() => ReducesTaxableBase = true;

    public void LinkAccount(Guid accountId) => AccountId = accountId == Guid.Empty ? null : accountId;

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();

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
