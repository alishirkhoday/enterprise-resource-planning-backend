namespace ERP.Domain.Entities.Common;

/// <summary>
/// A province, under a country and above its cities.
/// </summary>
public sealed class Province : BaseEntity
{
    public Country Country { get; private set; } = default!;
    public Guid CountryId { get; private set; }

    public string Name { get; private set; } = default!;
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<City> _cities = [];
    public IReadOnlyList<City> Cities => _cities;

    private Province() { }

    public Province(Country country, string name, string? code = null)
    {
        ArgumentNullException.ThrowIfNull(country, nameof(country));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Country = country;
        CountryId = country.Id;
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

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
