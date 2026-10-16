namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// One step of a routing: what is done, where, how long it takes to set up and how long it runs.
/// </summary>
public sealed class RoutingOperation : BaseEntity
{
    public Routing Routing { get; private set; } = default!;
    public Guid RoutingId { get; private set; }

    /// <summary>Order of the step, normally in tens so steps can be inserted between.</summary>
    public int SequenceNumber { get; private set; }

    public string Name { get; private set; } = default!;

    public WorkCenter WorkCenter { get; private set; } = default!;
    public Guid WorkCenterId { get; private set; }

    /// <summary>Fixed time to prepare, regardless of how many units are made.</summary>
    public decimal SetupHours { get; private set; }

    /// <summary>Time to make the routing base quantity.</summary>
    public decimal RunHours { get; private set; }

    /// <summary>Waiting time after the step before the next one can start.</summary>
    public decimal QueueHours { get; private set; }

    /// <summary>Whether the step must pass a quality check before the next one starts.</summary>
    public bool RequiresQualityCheck { get; private set; }

    /// <summary>Expected loss at this step, which planning inflates the input quantity for.</summary>
    public Percentage ScrapPercentage { get; private set; } = default!;

    public string? Instructions { get; private set; }

    private RoutingOperation() { }

    public RoutingOperation(Routing routing, int sequenceNumber, string name, WorkCenter workCenter, decimal setupHours, decimal runHours)
    {
        ArgumentNullException.ThrowIfNull(routing, nameof(routing));
        ArgumentNullException.ThrowIfNull(workCenter, nameof(workCenter));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequenceNumber, nameof(sequenceNumber));
        ArgumentOutOfRangeException.ThrowIfNegative(setupHours, nameof(setupHours));
        ArgumentOutOfRangeException.ThrowIfNegative(runHours, nameof(runHours));

        Routing = routing;
        RoutingId = routing.Id;
        SequenceNumber = sequenceNumber;
        Name = name.Trim();
        WorkCenter = workCenter;
        WorkCenterId = workCenter.Id;
        SetupHours = setupHours;
        RunHours = runHours;
        ScrapPercentage = Percentage.Zero();
    }

    public void SetQueueHours(decimal queueHours)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(queueHours, nameof(queueHours));
        QueueHours = queueHours;
    }

    public void SetScrapPercentage(Percentage scrapPercentage)
    {
        ArgumentNullException.ThrowIfNull(scrapPercentage, nameof(scrapPercentage));
        ScrapPercentage = scrapPercentage;
    }

    public void RequireQualityCheck() => RequiresQualityCheck = true;

    public void SetInstructions(string? instructions) =>
        Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim();

    /// <summary>Total hours this step takes for a given quantity.</summary>
    public decimal HoursFor(decimal quantity, decimal baseQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseQuantity, nameof(baseQuantity));
        return SetupHours + RunHours * (quantity / baseQuantity) + QueueHours;
    }

    /// <summary>The cost of running this step for a given quantity, when the centre has a rate.</summary>
    public Money? CostFor(decimal quantity, decimal baseQuantity) =>
        WorkCenter.CostOf(HoursFor(quantity, baseQuantity));
}
