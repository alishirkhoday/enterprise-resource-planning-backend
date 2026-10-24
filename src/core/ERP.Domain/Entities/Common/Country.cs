namespace ERP.Domain.Entities.Common;

/// <summary>
/// A country, with the dialling prefix its phone numbers normalize against.
/// </summary>
public sealed class Country : AggregateRoot
{
    /// <summary>ISO 3166-1 alpha-2 code, for example IR.</summary>
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    /// <summary>International dialling prefix without the plus sign, for example 98.</summary>
    public string? PhoneRegionCode { get; private set; }

    public bool IsActive { get; private set; }

    private readonly List<Province> _provinces = [];
    public IReadOnlyList<Province> Provinces => _provinces.AsReadOnly();

    private Country() { }

    public Country(string code, string name, string? phoneRegionCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        PhoneRegionCode = string.IsNullOrWhiteSpace(phoneRegionCode) ? null : phoneRegionCode.Trim();
        IsActive = true;
    }

    public void Update(string name, string? phoneRegionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        PhoneRegionCode = string.IsNullOrWhiteSpace(phoneRegionCode) ? null : phoneRegionCode.Trim();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
