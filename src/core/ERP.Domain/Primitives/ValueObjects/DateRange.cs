namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>
/// A closed date interval. EndDate is inclusive; a null end means the range is open ended.
/// </summary>
public sealed class DateRange : ValueObject
{
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    private DateRange() { }

    private DateRange(DateOnly startDate, DateOnly? endDate)
    {
        if (endDate.HasValue && endDate.Value < startDate)
            throw new ArgumentOutOfRangeException(nameof(endDate), "EndDate cannot be earlier than StartDate.");
        StartDate = startDate;
        EndDate = endDate;
    }

    public static DateRange Create(DateOnly startDate, DateOnly? endDate = null) => new(startDate, endDate);

    public bool IsOpenEnded => EndDate is null;

    public int? TotalDays => EndDate.HasValue ? EndDate.Value.DayNumber - StartDate.DayNumber + 1 : null;

    public bool Contains(DateOnly date) => date >= StartDate && (EndDate is null || date <= EndDate.Value);

    public bool Overlaps(DateRange other)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));
        var thisEnd = EndDate ?? DateOnly.MaxValue;
        var otherEnd = other.EndDate ?? DateOnly.MaxValue;
        return StartDate <= otherEnd && other.StartDate <= thisEnd;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}
