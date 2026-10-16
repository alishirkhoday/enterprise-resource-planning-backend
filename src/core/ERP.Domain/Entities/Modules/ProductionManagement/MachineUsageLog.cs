namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// How long a machine actually ran. The hours maintenance is scheduled from, since a machine
/// that sat idle for a month does not need servicing for it.
/// </summary>
public sealed class MachineUsageLog : BaseEntity
{
    public Machine Machine { get; private set; } = default!;
    public ProductionTask Task { get; private set; } = default!;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }
    public string? Notes { get; private set; }

    // Required by Entity Framework to materialise the entity.
    private MachineUsageLog() { }

    public MachineUsageLog(Machine machine, ProductionTask task, DateTimeOffset startTime, string? notes = null)
    {
        Machine = machine;
        Task = task;
        StartTime = startTime;
        Notes = notes;
    }

    public void End(DateTimeOffset endTime)
    {
        EndTime = endTime;
    }
}
