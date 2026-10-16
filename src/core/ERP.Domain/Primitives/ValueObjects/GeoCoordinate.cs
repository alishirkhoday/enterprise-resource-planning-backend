namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>
/// A point on the earth, held as decimal degrees. Used for a warehouse gate, a delivery address
/// or wherever a vehicle reported itself.
/// <para>
/// The ranges are checked on the way in. A swapped pair of coordinates is the commonest mistake
/// here and only half of it is catchable - a latitude above ninety is impossible, whereas two
/// numbers that are both valid but the wrong way round will pass and put the point in the sea.
/// </para>
/// </summary>
public sealed class GeoCoordinate : ValueObject
{
    /// <summary>North-south, between -90 and 90.</summary>
    public decimal Latitude { get; private set; }

    /// <summary>East-west, between -180 and 180.</summary>
    public decimal Longitude { get; private set; }

    private GeoCoordinate() { }

    private GeoCoordinate(decimal latitude, decimal longitude)
    {
        if (latitude < -90m || latitude > 90m)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude < -180m || longitude > 180m)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        Latitude = latitude;
        Longitude = longitude;
    }

    public static GeoCoordinate Create(decimal latitude, decimal longitude) => new(latitude, longitude);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }

    public override string ToString() => $"{Latitude},{Longitude}";
}
