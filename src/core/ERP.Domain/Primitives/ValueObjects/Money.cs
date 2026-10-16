namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>
/// An amount of money in a specific currency. Arithmetic between different currencies is rejected;
/// convert through an exchange rate first.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    private Money() { }

    private Money(decimal amount, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        Amount = amount;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
    }

    public static Money Create(decimal amount, string currencyCode) => new(amount, currencyCode);

    public static Money Zero(string currencyCode) => new(0m, currencyCode);

    public bool IsZero => Amount == 0m;
    public bool IsPositive => Amount > 0m;
    public bool IsNegative => Amount < 0m;

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, CurrencyCode);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, CurrencyCode);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, CurrencyCode);

    public Money Divide(decimal divisor)
    {
        if (divisor == 0m)
            throw new DivideByZeroException("Cannot divide money by zero.");
        return new Money(Amount / divisor, CurrencyCode);
    }

    public Money Negate() => new(-Amount, CurrencyCode);

    /// <summary>
    /// Applies a percentage and returns the resulting portion, not the remainder.
    /// </summary>
    public Money Percent(Percentage percentage)
    {
        ArgumentNullException.ThrowIfNull(percentage, nameof(percentage));
        return new Money(Amount * percentage.AsFraction(), CurrencyCode);
    }

    public Money Round(int decimals) => new(Math.Round(Amount, decimals, MidpointRounding.AwayFromZero), CurrencyCode);

    /// <summary>
    /// Converts to another currency using a rate expressed as units of target per one unit of source.
    /// </summary>
    public Money ConvertTo(string targetCurrencyCode, decimal rate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCurrencyCode, nameof(targetCurrencyCode));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rate, nameof(rate));
        return new Money(Amount * rate, targetCurrencyCode);
    }

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));
        if (!string.Equals(CurrencyCode, other.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Currency mismatch between {CurrencyCode} and {other.CurrencyCode}.");
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money left, decimal factor) => left.Multiply(factor);
    public static Money operator /(Money left, decimal divisor) => left.Divide(divisor);
    public static Money operator -(Money value) => value.Negate();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return CurrencyCode;
    }

    public override string ToString() => $"{Amount} {CurrencyCode}";
}
