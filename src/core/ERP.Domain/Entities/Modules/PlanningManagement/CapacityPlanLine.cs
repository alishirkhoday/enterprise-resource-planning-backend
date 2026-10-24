using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// One work centre in one bucket: the hours it can give and the hours that have been asked of it.
/// </summary>
public sealed class CapacityPlanLine : BaseEntity
{
    public CapacityPlan Plan { get; private set; } = default!;
    public Guid PlanId { get; private set; }

    public WorkCenter WorkCenter { get; private set; } = default!;
    public Guid WorkCenterId { get; private set; }

    public DateOnly BucketStartDate { get; private set; }
    public DateOnly BucketEndDate { get; private set; }

    /// <summary>Hours the calendar says exist, before any allowance for reality.</summary>
    public decimal AvailableHours { get; private set; }

    /// <summary>
    /// How much of those hours the centre historically converts into work. A centre that is idle a
    /// fifth of the time is not a centre with fewer hours; it is one whose hours are worth less,
    /// and planning against the raw figure guarantees an overload that nobody sees coming.
    /// </summary>
    public Percentage Efficiency { get; private set; } = default!;

    public decimal EffectiveHours => AvailableHours * Efficiency.Value / 100m;

    /// <summary>Hours already committed by released orders.</summary>
    public decimal FirmLoadHours { get; private set; }

    /// <summary>Hours that planning proposes to add on top.</summary>
    public decimal PlannedLoadHours { get; private set; }

    public decimal TotalLoadHours => FirmLoadHours + PlannedLoadHours;

    public string? Description { get; private set; }

    private CapacityPlanLine() { }

    public CapacityPlanLine(CapacityPlan plan, WorkCenter workCenter, DateOnly bucketStartDate, DateOnly bucketEndDate, decimal availableHours)
    {
        ArgumentNullException.ThrowIfNull(plan, nameof(plan));
        ArgumentNullException.ThrowIfNull(workCenter, nameof(workCenter));
        ArgumentOutOfRangeException.ThrowIfNegative(availableHours, nameof(availableHours));
        if (bucketEndDate < bucketStartDate)
            throw new ArgumentException("A bucket cannot end before it starts.", nameof(bucketEndDate));

        Plan = plan;
        PlanId = plan.Id;
        WorkCenter = workCenter;
        WorkCenterId = workCenter.Id;
        BucketStartDate = bucketStartDate;
        BucketEndDate = bucketEndDate;
        AvailableHours = availableHours;
        Efficiency = Percentage.Create(100m);
    }

    internal void SetEfficiency(Percentage efficiency)
    {
        ArgumentNullException.ThrowIfNull(efficiency, nameof(efficiency));
        Efficiency = efficiency;
    }

    internal void RecordLoad(decimal firmLoadHours, decimal plannedLoadHours)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(firmLoadHours, nameof(firmLoadHours));
        ArgumentOutOfRangeException.ThrowIfNegative(plannedLoadHours, nameof(plannedLoadHours));
        FirmLoadHours = firmLoadHours;
        PlannedLoadHours = plannedLoadHours;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public decimal OverloadHours => Math.Max(0m, TotalLoadHours - EffectiveHours);

    public decimal SpareHours => Math.Max(0m, EffectiveHours - TotalLoadHours);

    public bool IsOverloaded => TotalLoadHours > EffectiveHours;

    /// <summary>Share of the effective hours that is spoken for.</summary>
    public decimal UtilizationPercentage => EffectiveHours == 0m ? 0m : TotalLoadHours / EffectiveHours * 100m;
}
