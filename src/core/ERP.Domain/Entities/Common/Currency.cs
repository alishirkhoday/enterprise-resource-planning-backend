namespace ERP.Domain.Entities.Common;

/// <summary>
/// A currency the system can hold amounts in. Exactly one currency should be marked as the base
/// currency; every financial report is expressed in it.
/// </summary>
public sealed class Currency : AggregateRoot
{
    /// <summary>ISO 4217 code, for example IRR or USD.</summary>
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Symbol { get; private set; }

    /// <summary>How many decimal places amounts are rounded to. IRR uses 0, USD uses 2.</summary>
    public int DecimalPlaces { get; private set; }

    public bool IsBaseCurrency { get; private set; }
    public bool IsActive { get; private set; }

    private Currency() { }

    public Currency(string code, string name, int decimalPlaces = 0, string? symbol = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ValidateDetails(name, decimalPlaces);
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        DecimalPlaces = decimalPlaces;
        Symbol = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim();
        IsActive = true;
    }

    public void Update(string name, int decimalPlaces, string? symbol)
    {
        ValidateDetails(name, decimalPlaces);
        Name = name.Trim();
        DecimalPlaces = decimalPlaces;
        Symbol = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim();
    }

    public void MarkAsBaseCurrency() => IsBaseCurrency = true;
    public void UnmarkAsBaseCurrency() => IsBaseCurrency = false;

    public void Activate() => IsActive = true;

    public void Deactivate()
    {
        if (IsBaseCurrency)
            throw new InvalidOperationException("The base currency cannot be deactivated.");
        IsActive = false;
    }

    /// <summary>Builds an amount in this currency.</summary>
    public Money Amount(decimal value) => Money.Create(value, Code);

    public Money ZeroAmount() => Money.Zero(Code);
    private static void ValidateDetails(string name, int decimalPlaces)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegative(decimalPlaces, nameof(decimalPlaces));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(decimalPlaces, 6, nameof(decimalPlaces));
    }
}
