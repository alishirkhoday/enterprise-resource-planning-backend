namespace ERP.Domain.Entities.Users.ValueObjects;

/// <summary>
/// An email address, normalized so two spellings of one address never become two accounts.
/// </summary>
public sealed class Email : ValueObject
{
    public string Value { get; } = default!;

    private Email()
    {
    }

    private Email(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var v = value.Trim();
        if (v.Contains(' ') || !v.Contains('@') || v.Length > 254)
        {
            throw new ArgumentException("Invalid email.", nameof(value));
        }
        Value = v;
    }

    public static Email Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
