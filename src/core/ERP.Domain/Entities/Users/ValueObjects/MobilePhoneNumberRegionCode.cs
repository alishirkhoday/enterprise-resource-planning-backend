namespace ERP.Domain.Entities.Users.ValueObjects;

/// <summary>
/// The country dialling prefix a mobile number belongs to, held apart so the national part
/// can be normalized against it.
/// </summary>
public sealed class MobilePhoneNumberRegionCode : ValueObject
{
    public string Value { get; } = default!;

    private MobilePhoneNumberRegionCode()
    {
    }

    private MobilePhoneNumberRegionCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var v = value.Trim();
        if (v.StartsWith("+"))
        {
            v = v[1..];
        }
        v = new string(v.Where(char.IsDigit).ToArray());
        if (v.Length < 1 || v.Length > 4)
        {
            throw new ArgumentException("Invalid region code.", nameof(value));
        }
        Value = v;
    }

    public static MobilePhoneNumberRegionCode Create(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
