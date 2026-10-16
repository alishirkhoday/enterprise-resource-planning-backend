namespace ERP.Domain.Entities.Common;

/// <summary>
/// A day the business does not work. Part of a calendar rather than global, because branches
/// and contractors do not always share one.
/// </summary>
public sealed class Holiday : BaseEntity
{
    public WorkCalendar Calendar { get; private set; } = default!;
    public Guid CalendarId { get; private set; }

    public DateOnly Date { get; private set; }
    public string Title { get; private set; } = default!;
    public bool IsOfficial { get; private set; }
    public string? Description { get; private set; }

    private Holiday() { }

    public Holiday(WorkCalendar calendar, DateOnly date, string title, bool isOfficial = true)
    {
        ArgumentNullException.ThrowIfNull(calendar, nameof(calendar));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Calendar = calendar;
        CalendarId = calendar.Id;
        Date = date;
        Title = title.Trim();
        IsOfficial = isOfficial;
    }

    public void Update(string title, bool isOfficial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Title = title.Trim();
        IsOfficial = isOfficial;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
