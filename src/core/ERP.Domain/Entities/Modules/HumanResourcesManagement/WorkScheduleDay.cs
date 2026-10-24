namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// One day of a working pattern - when somebody is expected in, and for how long.
/// </summary>
public sealed class WorkScheduleDay : BaseEntity
{
    public WorkSchedule Schedule { get; private set; } = default!;
    public Guid ScheduleId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }
    public bool IsWorkingDay { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public int BreakMinutes { get; private set; }

    private WorkScheduleDay() { }

    public WorkScheduleDay(WorkSchedule schedule, DayOfWeek dayOfWeek, TimeOnly? startTime, TimeOnly? endTime, int breakMinutes = 0)
    {
        ArgumentNullException.ThrowIfNull(schedule, nameof(schedule));
        ArgumentOutOfRangeException.ThrowIfNegative(breakMinutes, nameof(breakMinutes));
        if (startTime.HasValue != endTime.HasValue)
            throw new ArgumentException("Start and end time must both be provided or both omitted.", nameof(endTime));

        Schedule = schedule;
        ScheduleId = schedule.Id;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes;
        IsWorkingDay = startTime.HasValue;
    }

    /// <summary>Net hours expected on this day, after subtracting the break.</summary>
    public decimal ExpectedHours
    {
        get
        {
            if (!IsWorkingDay || StartTime is null || EndTime is null)
                return 0m;

            var minutes = (EndTime.Value - StartTime.Value).TotalMinutes;
            if (minutes < 0)
                minutes += TimeSpan.FromDays(1).TotalMinutes; // an overnight shift
            return Math.Round((decimal)(minutes - BreakMinutes) / 60m, 2);
        }
    }

    internal void MarkAsRestDay()
    {
        IsWorkingDay = false;
        StartTime = null;
        EndTime = null;
        BreakMinutes = 0;
    }
}
