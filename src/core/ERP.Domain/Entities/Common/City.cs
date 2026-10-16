namespace ERP.Domain.Entities.Common;

/// <summary>
/// A city, under a province. Carries a coordinate so an address can be placed on a map without
/// geocoding the text every time.
/// </summary>
public sealed class City : BaseEntity
{
    public Province Province { get; private set; } = default!;
    public Guid ProvinceId { get; private set; }

    public string Name { get; private set; } = default!;
    public string? Code { get; private set; }
    public GeoCoordinate? Location { get; private set; }
    public bool IsActive { get; private set; }

    private City() { }

    public City(Province province, string name, string? code = null)
    {
        ArgumentNullException.ThrowIfNull(province, nameof(province));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Province = province;
        ProvinceId = province.Id;
        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        IsActive = true;
    }

    public void Update(string name, string? code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
    }

    public void SetLocation(GeoCoordinate location)
    {
        ArgumentNullException.ThrowIfNull(location, nameof(location));
        Location = location;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
