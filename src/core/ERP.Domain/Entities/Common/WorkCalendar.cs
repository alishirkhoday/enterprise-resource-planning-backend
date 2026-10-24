namespace ERP.Domain.Entities.Common;

/// <summary>
/// The working week and the holidays that apply to a branch or the whole organization.
/// Attendance, leave entitlement and due date calculations all read from here.
/// </summary>
public sealed partial class WorkCalendar : AggregateRoot, IBranchScoped
{
    public string Name { get; private set; } = default!;
    public Guid? BranchId { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Standard working hours per day, used to convert hourly leave into days.</summary>
    public decimal DailyWorkingHours { get; private set; }

    private readonly List<DayOfWeek> _weekendDays = [];
    public IReadOnlyList<DayOfWeek> WeekendDays => _weekendDays.AsReadOnly();

    private readonly List<Holiday> _holidays = [];
    public IReadOnlyList<Holiday> Holidays => _holidays.AsReadOnly();

    private WorkCalendar() { }

    public WorkCalendar(string name, decimal dailyWorkingHours = 8m)
    {
        ValidateDetails(name, dailyWorkingHours);
        Name = name.Trim();
        DailyWorkingHours = dailyWorkingHours;
        IsActive = true;
    }

    public void Update(string name, decimal dailyWorkingHours)
    {
        ValidateDetails(name, dailyWorkingHours);
        Name = name.Trim();
        DailyWorkingHours = dailyWorkingHours;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void MarkAsDefault() => IsDefault = true;
    public void UnmarkAsDefault() => IsDefault = false;

    public void SetWeekendDays(IEnumerable<DayOfWeek> days)
    {
        ArgumentNullException.ThrowIfNull(days, nameof(days));
        var replacement = days.Distinct().ToArray();
        if (replacement.Any(day => !Enum.IsDefined(day)))
            throw new ArgumentException("Every weekend day must be a valid day of the week.", nameof(days));
        _weekendDays.Clear();
        _weekendDays.AddRange(replacement);
    }

    public void AddHoliday(Holiday holiday)
    {
        ArgumentNullException.ThrowIfNull(holiday);
        EnsureCanAddChild(holiday, holiday.CalendarId, _holidays);

        if (_holidays.Any(h => h.Date == holiday.Date))
            throw new InvalidOperationException($"A holiday is already registered on {holiday.Date:yyyy/MM/dd}.");
        _holidays.Add(holiday);
    }

    public void RemoveHoliday(DateOnly date)
    {
        var holiday = _holidays.FirstOrDefault(h => h.Date == date);
        if (holiday is not null)
            _holidays.Remove(holiday);
    }

    public bool IsWorkingDay(DateOnly date)
    {
        if (_weekendDays.Contains(date.DayOfWeek))
            return false;
        return !_holidays.Any(h => h.Date == date);
    }

    /// <summary>Counts working days in the inclusive range, skipping weekends and holidays.</summary>
    public int CountWorkingDays(DateRange range)
    {
        ArgumentNullException.ThrowIfNull(range, nameof(range));
        if (range.EndDate is null)
            throw new ArgumentException("An open ended range has no finite working day count.", nameof(range));

        var count = 0;
        for (var date = range.StartDate; date <= range.EndDate.Value; date = date.AddDays(1))
        {
            if (IsWorkingDay(date))
                count++;
        }
        return count;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    private static void ValidateDetails(string name, decimal dailyWorkingHours)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dailyWorkingHours, nameof(dailyWorkingHours));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dailyWorkingHours, 24m, nameof(dailyWorkingHours));
    }
}
