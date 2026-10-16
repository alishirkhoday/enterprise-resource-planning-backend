namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// One person's clock record for one day - حضور و غیاب.
/// 
/// One row per day, opened on entry and closed on exit. Somebody who steps out for lunch and
/// comes back has not begun a second working day, which is why the exit updates the row that
/// is already there rather than creating another.
/// </summary>
public sealed class HumanAttendanceRecord : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly? EntryTime { get; private set; }
    public TimeOnly? ExitTime { get; private set; }

    private HumanAttendanceRecord() { }

    public HumanAttendanceRecord(Human human, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        Human = human;
        HumanId = human.Id;
        Date = date;
    }

    public void SetEntryTime(TimeOnly time)
    {
        EntryTime = time;
    }

    public void SetExitTime(TimeOnly time)
    {
        ExitTime = time;
    }
}
