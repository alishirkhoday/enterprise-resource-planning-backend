namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>
/// A percentage stored as a human readable value, so 9 means 9 percent. Bounded to 0..100.
/// </summary>
public sealed class Percentage : ValueObject
{
    public decimal Value { get; private set; }

    private Percentage() { }

    private Percentage(decimal value)
    {
        if (value < 0m || value > 100m)
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 100.");
        Value = value;
    }

    public static Percentage Create(decimal value) => new(value);
    public static Percentage Zero() => new(0m);

    /// <summary>
    /// Returns the value as a fraction, so 9 percent becomes 0.09.
    /// </summary>
    public decimal AsFraction() => Value / 100m;

    public decimal ApplyTo(decimal amount) => amount * AsFraction();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value}%";
}
