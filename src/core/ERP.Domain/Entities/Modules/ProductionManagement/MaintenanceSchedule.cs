namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// When a machine is next due for service, by time or by hours run.
/// </summary>
public sealed class MaintenanceSchedule : AggregateRoot
{
    public Machine Machine { get; private set; } = default!;
    public DateTimeOffset ScheduledDate { get; private set; }
    public string MaintenanceType { get; private set; } = default!; // Preventive / Corrective
    public string Description { get; private set; } = default!;
    public bool IsCompleted { get; private set; } = false;

    // Required by Entity Framework to materialise the entity.
    private MaintenanceSchedule() { }

    public MaintenanceSchedule(Machine machine, DateTimeOffset scheduledDate, string type, string description)
    {
        Machine = machine;
        ScheduledDate = scheduledDate;
        MaintenanceType = type;
        Description = description;
    }

    public void MarkAsCompleted()
    {
        IsCompleted = true;
    }
}
