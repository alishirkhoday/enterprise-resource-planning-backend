namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>
/// A passport number, for people who have no national identifier on file - a foreign employee or
/// a customer abroad. Kept apart from <see cref="NationalId"/> because the two are issued by
/// different authorities and are never interchangeable in a uniqueness check.
/// </summary>
public sealed class PassportId : ValueObject
{
    /// <summary>The passport number itself, trimmed.</summary>
    public string Value { get; } = default!;

    private PassportId()
    {
    }

    private PassportId(string value)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"value is null");
        }
        Value = value;
    }

    public static PassportId Create(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
