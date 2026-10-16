namespace ERP.Domain.Entities.Users.ValueObjects;

/// <summary>
/// A username in its normalized form. Case and surrounding space are removed on the way in,
/// because a login that depends on how somebody typed it is a login that sometimes fails.
/// </summary>
public sealed class Username : ValueObject
{
    public string Value { get; } = default!;

    private Username()
    {
    }

    private Username(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var v = value.Trim();
        if (v.Length < 3 || v.Length > 64)
        {
            throw new ArgumentException("Username length must be between 3 and 64 characters.", nameof(value));
        }
        Value = v;
    }

    public static Username Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
