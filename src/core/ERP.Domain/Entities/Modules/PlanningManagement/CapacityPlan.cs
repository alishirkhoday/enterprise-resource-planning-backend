namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// The other half of planning. Material requirements answer whether the parts will exist; this
/// answers whether anyone can actually work on them.
/// <para>
/// A plan that is feasible on materials and impossible on capacity is worse than no plan, because
/// it commits delivery dates the shop floor cannot meet. Keeping load against available hours per
/// work centre per bucket is what makes the overload visible while there is still time to move it.
/// </para>
/// </summary>
public sealed class CapacityPlan : BaseEntity, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public DateRange Horizon { get; private set; } = default!;
    public ForecastBucket Bucket { get; private set; }
    public CapacityPlanStatus Status { get; private set; }

    /// <summary>The material plan this capacity check belongs to, when it was run alongside one.</summary>
    public Guid? MaterialRequirementPlanId { get; private set; }

    public Guid? BranchId { get; private set; }

    public DateTimeOffset StartedOn { get; private set; }
    public DateTimeOffset? CompletedOn { get; private set; }
    public string? RunBy { get; private set; }
    public string? Description { get; private set; }

    private readonly List<CapacityPlanLine> _lines = [];
    public IReadOnlyList<CapacityPlanLine> Lines => _lines;

    private CapacityPlan() { }

    public CapacityPlan(string code, DateRange horizon, ForecastBucket bucket, string? runBy = null)
    {
        ArgumentNullException.ThrowIfNull(horizon, nameof(horizon));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        if (horizon.EndDate is null)
            throw new ArgumentException("A planning horizon must be closed at both ends.", nameof(horizon));

        Code = code.Trim();
        Horizon = horizon;
        Bucket = bucket;
        RunBy = runBy;
        StartedOn = DateTimeOffset.UtcNow;
        Status = CapacityPlanStatus.Running;
    }

    public void LinkToMaterialPlan(Guid materialRequirementPlanId) =>
        MaterialRequirementPlanId = materialRequirementPlanId == Guid.Empty ? null : materialRequirementPlanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddLine(CapacityPlanLine line)
    {
        ArgumentNullException.ThrowIfNull(line, nameof(line));
        if (Status != CapacityPlanStatus.Running)
            throw new InvalidOperationException($"Lines can only be written while the run is in progress; this one is {Status}.");
        if (_lines.Any(l => l.WorkCenterId == line.WorkCenterId && l.BucketStartDate == line.BucketStartDate))
            throw new InvalidOperationException("This work centre already has a figure for that bucket.");
        _lines.Add(line);
    }

    public void Complete()
    {
        if (Status != CapacityPlanStatus.Running)
            throw new InvalidOperationException($"A run in state {Status} cannot be completed.");
        Status = CapacityPlanStatus.Completed;
        CompletedOn = DateTimeOffset.UtcNow;
    }

    public void Fail(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != CapacityPlanStatus.Running)
            throw new InvalidOperationException($"A run in state {Status} cannot fail.");
        Status = CapacityPlanStatus.Failed;
        CompletedOn = DateTimeOffset.UtcNow;
        Description = reason.Trim();
    }

    /// <summary>Where more work is planned than there are hours to do it, worst first.</summary>
    public IReadOnlyList<CapacityPlanLine> OverloadedLines =>
        [.. _lines.Where(l => l.IsOverloaded).OrderByDescending(l => l.OverloadHours)];

    public bool IsFeasible => Status == CapacityPlanStatus.Completed && _lines.TrueForAll(l => !l.IsOverloaded);
}
