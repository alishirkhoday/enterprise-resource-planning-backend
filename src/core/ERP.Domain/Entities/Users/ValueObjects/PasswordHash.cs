namespace ERP.Domain.Entities.Users.ValueObjects;

/// <summary>
/// A hashed password. A value object rather than a string so a plain password can never be
/// assigned where a hash is expected.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    public string Value { get; } = default!;

    private PasswordHash()
    {
    }

    private PasswordHash(string value)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"value is null");
        }
        Value = value;
    }

    public static PasswordHash Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
