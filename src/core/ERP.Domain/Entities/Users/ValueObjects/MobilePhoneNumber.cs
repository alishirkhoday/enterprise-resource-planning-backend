namespace ERP.Domain.Entities.Users.ValueObjects;

/// <summary>
/// A mobile number in the single normalized form the system compares on. The trunk zero is
/// dropped, so 0912..., +98912... and 0098912... are all one value.
/// </summary>
public sealed class MobilePhoneNumber : ValueObject
{
    public string Value { get; } = default!;

    private MobilePhoneNumber()
    {
    }

    private MobilePhoneNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var v = new string(value.Trim().Where(char.IsDigit).ToArray());
        if (v.Length < 6 || v.Length > 15)
        {
            throw new ArgumentException("Invalid phone number.", nameof(value));
        }
        Value = v;
    }

    public static MobilePhoneNumber Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
