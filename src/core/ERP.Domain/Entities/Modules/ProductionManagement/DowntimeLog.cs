namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// A period a machine was not producing, and why. What an availability figure is built from.
/// </summary>
public sealed class DowntimeLog : BaseEntity
{
    public Machine Machine { get; private set; } = default!;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }
    public string Reason { get; private set; } = default!;

    // Required by Entity Framework to materialise the entity.
    private DowntimeLog() { }

    public DowntimeLog(Machine machine, DateTimeOffset startTime, string reason)
    {
        Machine = machine;
        StartTime = startTime;
        Reason = reason;
    }

    public void End(DateTimeOffset endTime)
    {
        EndTime = endTime;
    }
}
