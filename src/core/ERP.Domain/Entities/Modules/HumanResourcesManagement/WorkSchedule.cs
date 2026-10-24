namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// The pattern of hours an employee is expected to work. Attendance is judged against this,
/// and overtime is whatever falls outside it.
/// </summary>
public sealed partial class WorkSchedule : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public Guid? WorkCalendarId { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Minutes of tolerance before an arrival counts as late.</summary>
    public int LateToleranceMinutes { get; private set; }

    private readonly List<WorkScheduleDay> _days = [];
    public IReadOnlyList<WorkScheduleDay> Days => _days.AsReadOnly();

    private WorkSchedule() { }

    public WorkSchedule(string name, int lateToleranceMinutes = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegative(lateToleranceMinutes, nameof(lateToleranceMinutes));
        Name = name.Trim();
        LateToleranceMinutes = lateToleranceMinutes;
        IsActive = true;
    }

    public void Update(string name, int lateToleranceMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegative(lateToleranceMinutes, nameof(lateToleranceMinutes));
        Name = name.Trim();
        LateToleranceMinutes = lateToleranceMinutes;
    }

    public void AssignWorkCalendar(Guid workCalendarId) => WorkCalendarId = workCalendarId == Guid.Empty ? null : workCalendarId;

    public void AddDay(WorkScheduleDay day)
    {
        ArgumentNullException.ThrowIfNull(day);
        EnsureCanAddChild(day, day.ScheduleId, _days);

        if (_days.Any(d => d.DayOfWeek == day.DayOfWeek))
            throw new InvalidOperationException($"{day.DayOfWeek} is already defined in this schedule.");
        _days.Add(day);
    }

    public void RemoveDay(DayOfWeek dayOfWeek)
    {
        var day = _days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
        if (day is not null)
            _days.Remove(day);
    }

    public WorkScheduleDay? DayFor(DayOfWeek dayOfWeek) => _days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);

    public decimal WeeklyHours => _days.Sum(d => d.ExpectedHours);

    public void MarkAsDefault() => IsDefault = true;
    public void UnmarkAsDefault() => IsDefault = false;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
