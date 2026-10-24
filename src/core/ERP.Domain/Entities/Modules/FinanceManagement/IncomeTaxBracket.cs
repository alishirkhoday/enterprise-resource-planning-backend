namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// One band of the progressive payroll income tax table for a given year. Brackets are held as
/// data because the legislature revises them annually.
/// </summary>
public sealed class IncomeTaxBracket : AggregateRoot
{
    /// <summary>The tax year the band belongs to.</summary>
    public int Year { get; private set; }

    /// <summary>Order of the band within the table, starting at one.</summary>
    public short Order { get; private set; }

    /// <summary>Annual income at which this band starts.</summary>
    public Money FromAmount { get; private set; } = default!;

    /// <summary>Annual income at which this band ends. Null means the band is open ended.</summary>
    public Money? ToAmount { get; private set; }

    public Percentage Rate { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private IncomeTaxBracket() { }

    public IncomeTaxBracket(int year, short order, Money fromAmount, Money? toAmount, Percentage rate)
    {
        ArgumentNullException.ThrowIfNull(fromAmount, nameof(fromAmount));
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order, nameof(order));
        if (year < 1300 || year > 2200)
            throw new ArgumentOutOfRangeException(nameof(year), "Year is out of range.");
        if (fromAmount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(fromAmount), "The band cannot start below zero.");
        if (toAmount is not null && toAmount.Amount <= fromAmount.Amount)
            throw new ArgumentOutOfRangeException(nameof(toAmount), "The band must end above where it starts.");

        Year = year;
        Order = order;
        FromAmount = fromAmount;
        ToAmount = toAmount;
        Rate = rate;
        IsActive = true;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public bool Covers(Money income)
    {
        ArgumentNullException.ThrowIfNull(income, nameof(income));
        return income.Amount >= FromAmount.Amount && (ToAmount is null || income.Amount <= ToAmount.Amount);
    }

    /// <summary>The tax this band alone contributes for the given income.</summary>
    public Money TaxOn(Money income)
    {
        ArgumentNullException.ThrowIfNull(income, nameof(income));
        if (income.Amount <= FromAmount.Amount)
            return Money.Zero(income.CurrencyCode);

        var ceiling = ToAmount is null ? income.Amount : Math.Min(income.Amount, ToAmount.Amount);
        var amountInBand = ceiling - FromAmount.Amount;
        return Money.Create(amountInBand, income.CurrencyCode).Percent(Rate);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
