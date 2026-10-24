namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A tax the organization charges or withholds, at a rate that is valid for a period. Rates are
/// versioned rather than overwritten, so an old invoice can still be explained.
/// </summary>
public sealed class Tax : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public TaxType Type { get; private set; }

    public Percentage Rate { get; private set; } = default!;

    /// <summary>The window this rate applies to. An open end means it is the current rate.</summary>
    public DateRange EffectivePeriod { get; private set; } = default!;

    /// <summary>When true the price already contains the tax and it must be extracted, not added.</summary>
    public bool IsInclusive { get; private set; }

    public Guid? PayableAccountId { get; private set; }
    public Guid? ReceivableAccountId { get; private set; }

    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>The numeric rate, for callers that need it without the value object.</summary>
    public decimal RateValue => Rate.Value;

    private Tax() { }

    public Tax(string code, string title, TaxType type, Percentage rate, DateRange effectivePeriod)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        ArgumentNullException.ThrowIfNull(effectivePeriod, nameof(effectivePeriod));

        Code = code.Trim();
        Title = title.Trim();
        Type = type;
        Rate = rate;
        EffectivePeriod = effectivePeriod;
        IsActive = true;
    }

    public void Update(string title, Percentage rate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        Title = title.Trim();
        Rate = rate;
    }

    public void MarkAsInclusive() => IsInclusive = true;
    public void MarkAsExclusive() => IsInclusive = false;

    public void LinkAccounts(Guid? payableAccountId, Guid? receivableAccountId)
    {
        PayableAccountId = payableAccountId == Guid.Empty ? null : payableAccountId;
        ReceivableAccountId = receivableAccountId == Guid.Empty ? null : receivableAccountId;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Ends this rate so a successor can take over from the next day.</summary>
    public void Supersede(DateOnly lastEffectiveDate)
    {
        EffectivePeriod = DateRange.Create(EffectivePeriod.StartDate, lastEffectiveDate);
        IsActive = false;
    }

    public bool IsEffectiveOn(DateOnly date) => IsActive && EffectivePeriod.Contains(date);

    /// <summary>
    /// The tax carried by an amount. An inclusive tax is extracted from the amount, an exclusive
    /// one is added on top of it.
    /// </summary>
    public Money CalculateOn(Money baseAmount)
    {
        ArgumentNullException.ThrowIfNull(baseAmount, nameof(baseAmount));
        if (IsInclusive)
        {
            var fraction = Rate.AsFraction();
            return baseAmount.Multiply(fraction / (1m + fraction));
        }
        return baseAmount.Percent(Rate);
    }

    /// <summary>The amount before tax, for a price that already includes it.</summary>
    public Money NetOf(Money grossAmount)
    {
        ArgumentNullException.ThrowIfNull(grossAmount, nameof(grossAmount));
        return IsInclusive ? grossAmount.Subtract(CalculateOn(grossAmount)) : grossAmount;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
