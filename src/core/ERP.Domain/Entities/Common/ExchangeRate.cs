namespace ERP.Domain.Entities.Common;

/// <summary>
/// The rate to convert one currency into another on a given date. Rates are kept as history
/// rather than overwritten, so past documents can always be revalued with the rate that applied then.
/// </summary>
public sealed class ExchangeRate : BaseEntity
{
    public Currency FromCurrency { get; private set; } = default!;
    public Guid FromCurrencyId { get; private set; }

    public Currency ToCurrency { get; private set; } = default!;
    public Guid ToCurrencyId { get; private set; }

    /// <summary>Units of <see cref="ToCurrency"/> for one unit of <see cref="FromCurrency"/>.</summary>
    public decimal Rate { get; private set; }

    public DateOnly EffectiveDate { get; private set; }
    public string? Source { get; private set; }

    private ExchangeRate() { }

    public ExchangeRate(Currency fromCurrency, Currency toCurrency, decimal rate, DateOnly effectiveDate, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(fromCurrency, nameof(fromCurrency));
        ArgumentNullException.ThrowIfNull(toCurrency, nameof(toCurrency));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rate, nameof(rate));
        if (fromCurrency.Id == toCurrency.Id)
            throw new ArgumentException("Source and target currency cannot be the same.", nameof(toCurrency));

        FromCurrency = fromCurrency;
        FromCurrencyId = fromCurrency.Id;
        ToCurrency = toCurrency;
        ToCurrencyId = toCurrency.Id;
        Rate = rate;
        EffectiveDate = effectiveDate;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
    }

    public void UpdateRate(decimal rate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rate, nameof(rate));
        Rate = rate;
    }

    public Money Convert(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!string.Equals(amount.CurrencyCode, FromCurrency.Code, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"This rate converts from {FromCurrency.Code}, not {amount.CurrencyCode}.");
        return amount.ConvertTo(ToCurrency.Code, Rate);
    }
}
