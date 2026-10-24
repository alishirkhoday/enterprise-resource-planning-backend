using System.Globalization;

namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>An immutable postal snapshot, including its optional geography references.</summary>
public sealed class Address : ValueObject
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
    public string? Unit { get; private set; }
    public string PostalCode { get; private set; } = default!;

    private Address() { }

    public Address(string country, string province, string city, string region, string street,
        string plaque, string postalCode, string? unit = null, Guid? countryId = null,
        Guid? provinceId = null, Guid? cityId = null, string? latitude = null, string? longitude = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentException.ThrowIfNullOrWhiteSpace(province);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(plaque);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        if (latitude is not null || longitude is not null)
        {
            if (!decimal.TryParse(latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                throw new ArgumentException("Latitude must be a decimal coordinate.", nameof(latitude));
            if (!decimal.TryParse(longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                throw new ArgumentException("Longitude must be a decimal coordinate.", nameof(longitude));
            var coordinate = GeoCoordinate.Create(lat, lon);
            latitude = coordinate.Latitude.ToString(CultureInfo.InvariantCulture);
            longitude = coordinate.Longitude.ToString(CultureInfo.InvariantCulture);
        }
        Country = country.Trim();
        Province = province.Trim();
        City = city.Trim();
        Region = region.Trim();
        Street = street.Trim();
        Plaque = plaque.Trim();
        PostalCode = postalCode.Trim();
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        CountryId = countryId == Guid.Empty ? null : countryId;
        ProvinceId = provinceId == Guid.Empty ? null : provinceId;
        CityId = cityId == Guid.Empty ? null : cityId;
        Latitude = latitude;
        Longitude = longitude;
    }

    // EF owned values need a distinct instance for each persisted owner.
    internal Address Copy() =>
        new(Country, Province, City, Region, Street, Plaque, PostalCode, Unit,
            CountryId, ProvinceId, CityId, Latitude, Longitude);

    public Address WithGeographicCoordinates(string latitude, string longitude) =>
        new(Country, Province, City, Region, Street, Plaque, PostalCode, Unit,
            CountryId, ProvinceId, CityId, latitude, longitude);

    public Address WithGeographyReferences(Guid? countryId, Guid? provinceId, Guid? cityId) =>
        new(Country, Province, City, Region, Street, Plaque, PostalCode, Unit,
            countryId, provinceId, cityId, Latitude, Longitude);

    public Address WithUnit(string? unit) =>
        new(Country, Province, City, Region, Street, Plaque, PostalCode, unit,
            CountryId, ProvinceId, CityId, Latitude, Longitude);

    public string ToSingleLine()
    {
        var unitPart = Unit is null ? string.Empty : $", Unit {Unit}";
        return $"{Country}, {Province}, {City}, {Region}, {Street}, No {Plaque}{unitPart}, {PostalCode}";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return CountryId;
        yield return ProvinceId;
        yield return CityId;
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
