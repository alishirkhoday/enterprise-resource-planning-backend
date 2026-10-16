namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>
/// A postal address. The place names are stored as text alongside the optional master data
/// identifiers, so an address printed on an old document keeps reading the way it was issued
/// even after a city is renamed.
/// </summary>
public class Address : ValueObject
{
    public string? Latitude { get; private set; }
    public string? Longitude { get; private set; }

    public Guid? CountryId { get; private set; }
    public Guid? ProvinceId { get; private set; }
    public Guid? CityId { get; private set; }

    public string Country { get; private set; } = default!;
    public string Province { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string Region { get; private set; } = default!;
    public string Street { get; private set; } = default!;
    public string Plaque { get; private set; } = default!;

    /// <summary>Apartment or unit number, when the plaque alone is not enough.</summary>
    public string? Unit { get; private set; }

    public string PostalCode { get; private set; } = default!;

    private Address()
    {
    }

    public Address(string country, string province, string city, string region, string street, string plaque, string postalCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(country, nameof(country));
        ArgumentException.ThrowIfNullOrEmpty(province, nameof(province));
        ArgumentException.ThrowIfNullOrEmpty(city, nameof(city));
        ArgumentException.ThrowIfNullOrEmpty(region, nameof(region));
        ArgumentException.ThrowIfNullOrEmpty(street, nameof(street));
        ArgumentException.ThrowIfNullOrEmpty(plaque, nameof(plaque));
        ArgumentException.ThrowIfNullOrEmpty(postalCode, nameof(postalCode));
        Country = country.Trim();
        Province = province.Trim();
        City = city.Trim();
        Region = region.Trim();
        Street = street.Trim();
        Plaque = plaque.Trim();
        PostalCode = postalCode.Trim();
    }

    public void SetGeographicCoordinates(string latitude, string longitude)
    {
        ArgumentException.ThrowIfNullOrEmpty(latitude, nameof(latitude));
        ArgumentException.ThrowIfNullOrEmpty(longitude, nameof(longitude));
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Links the address to the geography master data.</summary>
    public void SetGeographyReferences(Guid? countryId, Guid? provinceId, Guid? cityId)
    {
        CountryId = countryId == Guid.Empty ? null : countryId;
        ProvinceId = provinceId == Guid.Empty ? null : provinceId;
        CityId = cityId == Guid.Empty ? null : cityId;
    }

    public void SetUnit(string? unit) => Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();

    public string ToSingleLine()
    {
        var unitPart = string.IsNullOrWhiteSpace(Unit) ? string.Empty : $", Unit {Unit}";
        return $"{Country}, {Province}, {City}, {Region}, {Street}, No {Plaque}{unitPart}, {PostalCode}";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Country;
        yield return Province;
        yield return City;
        yield return Region;
        yield return Street;
        yield return Plaque;
        yield return Unit;
        yield return PostalCode;
    }

    public override string ToString() => ToSingleLine();
}
