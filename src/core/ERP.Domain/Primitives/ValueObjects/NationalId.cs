namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>
/// The identifier a natural person is registered under - کد ملی in Iran.
/// <para>
/// Held as a value rather than a plain string so a person's national identifier cannot be
/// assigned from any string that happens to be lying around, and so the uniqueness checks that
/// depend on it always compare the same normalized form.
/// </para>
/// </summary>
public sealed class NationalId : ValueObject
{
    /// <summary>The identifier itself, trimmed.</summary>
    public string Value { get; } = default!;

    private NationalId()
    {
    }

    private NationalId(string value)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"value is null");
        }
        Value = value;
    }

    public static NationalId Create(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
