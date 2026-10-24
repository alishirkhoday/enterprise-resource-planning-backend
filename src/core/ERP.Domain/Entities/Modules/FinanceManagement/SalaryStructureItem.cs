namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// One line of a salary structure, such as the base wage, a housing allowance or the insurance
/// deduction, together with how its amount is arrived at.
/// </summary>
public sealed class SalaryStructureItem : BaseEntity
{
    public SalaryStructure Structure { get; private set; } = default!;
    public Guid StructureId { get; private set; }

    public string Code { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public SalaryItemNature Nature { get; private set; }
    public SalaryItemCalculation Calculation { get; private set; }

    /// <summary>Runs low to high, so a percentage item can build on the ones before it.</summary>
    public int CalculationOrder { get; private set; }

    /// <summary>The amount, for an item that is simply a fixed sum.</summary>
    public Money? FixedAmount { get; private set; }

    /// <summary>The rate, for an item computed as a share of something else.</summary>
    public Percentage? Rate { get; private set; }

    /// <summary>The item this one is computed from, when it is a percentage of another line.</summary>
    public string? BaseItemCode { get; private set; }

    /// <summary>Whether the item counts towards the base that income tax is computed on.</summary>
    public bool IsTaxable { get; private set; }

    /// <summary>Whether the item counts towards the base that social insurance is computed on.</summary>
    public bool IsInsurable { get; private set; }

    public Guid? AccountId { get; private set; }
    public string? Description { get; private set; }

    private SalaryStructureItem() { }

    public SalaryStructureItem(SalaryStructure structure, string code, string title, SalaryItemNature nature, SalaryItemCalculation calculation, int calculationOrder)
    {
        ArgumentNullException.ThrowIfNull(structure, nameof(structure));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegative(calculationOrder, nameof(calculationOrder));

        Structure = structure;
        StructureId = structure.Id;
        Code = code.Trim();
        Title = title.Trim();
        Nature = nature;
        Calculation = calculation;
        CalculationOrder = calculationOrder;
    }

    internal void SetFixedAmount(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (Calculation != SalaryItemCalculation.FixedAmount)
            throw new InvalidOperationException("A fixed amount only applies to an item calculated as a fixed amount.");
        if (amount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(amount), "A salary item cannot be negative.");
        FixedAmount = amount;
    }

    internal void SetRate(Percentage rate, string? baseItemCode = null)
    {
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        if (Calculation is not (SalaryItemCalculation.PercentageOfBase or SalaryItemCalculation.PercentageOfItem))
            throw new InvalidOperationException("A rate only applies to a percentage based item.");
        if (Calculation == SalaryItemCalculation.PercentageOfItem && string.IsNullOrWhiteSpace(baseItemCode))
            throw new ArgumentNullException(nameof(baseItemCode), "A percentage of another item needs that item code.");
        Rate = rate;
        BaseItemCode = string.IsNullOrWhiteSpace(baseItemCode) ? null : baseItemCode.Trim();
    }

    internal void SetTaxTreatment(bool isTaxable, bool isInsurable)
    {
        IsTaxable = isTaxable;
        IsInsurable = isInsurable;
    }

    internal void LinkAccount(Guid accountId) => AccountId = accountId == Guid.Empty ? null : accountId;

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// Computes the item against a base amount. Items calculated per day or per hour are left to
    /// the payroll run, which knows the worked quantity.
    /// </summary>
    public Money CalculateOn(Money baseAmount)
    {
        ArgumentNullException.ThrowIfNull(baseAmount, nameof(baseAmount));
        return Calculation switch
        {
            SalaryItemCalculation.FixedAmount => FixedAmount ?? Money.Zero(baseAmount.CurrencyCode),
            SalaryItemCalculation.PercentageOfBase or SalaryItemCalculation.PercentageOfItem =>
                Rate is null ? Money.Zero(baseAmount.CurrencyCode) : baseAmount.Percent(Rate),
            _ => Money.Zero(baseAmount.CurrencyCode)
        };
    }
}
