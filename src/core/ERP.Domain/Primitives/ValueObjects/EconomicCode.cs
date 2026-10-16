namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>
/// The tax identifier a legal entity is registered under. In Iran this is the economic code.
/// </summary>
public sealed class EconomicCode : ValueObject
{
    public string Value { get; private set; } = default!;

    private EconomicCode() { }

    private EconomicCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        Value = value.Trim();
    }

    public static EconomicCode Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
