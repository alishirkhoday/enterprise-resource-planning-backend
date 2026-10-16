namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// What is planned to run, where and when.
/// </summary>
public sealed class ProductionSchedule : BaseEntity
{
    public ProductionOrder Order { get; private set; } = default!;
    public WorkStation WorkStation { get; private set; } = default!;
    public DateTimeOffset ScheduledStart { get; private set; }
    public DateTimeOffset ScheduledEnd { get; private set; }

    // Required by Entity Framework to materialise the entity.
    private ProductionSchedule() { }

    public ProductionSchedule(ProductionOrder order, WorkStation workStation, DateTimeOffset scheduledStart, DateTimeOffset scheduledEnd)
    {
        Order = order;
        WorkStation = workStation;
        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
    }
}
