namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// One run of material requirements planning: for every item in scope, what is needed, what is
/// already coming, and therefore what somebody has to order or make.
/// <para>
/// A run is a document rather than a live query on purpose. Planning takes a photograph of demand
/// and supply at a moment and proposes actions against it; if the numbers moved underneath, the
/// proposals would stop agreeing with each other and the buyer would be ordering against one
/// picture while the scheduler works from another.
/// </para>
/// <para>
/// Nothing here commits anything. Each line is a suggestion until somebody releases it into a
/// purchase requisition or a production order, and the line then remembers what it became.
/// </para>
/// </summary>
public sealed partial class MaterialRequirementPlan : AggregateRoot, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public DateRange Horizon { get; private set; } = default!;
    public MaterialRequirementPlanStatus Status { get; private set; }

    /// <summary>The forecast this run consumed, when demand was not taken from firm orders alone.</summary>
    public Guid? DemandForecastId { get; private set; }

    public Guid? WarehouseId { get; private set; }
    public Guid? BranchId { get; private set; }

    public DateTimeOffset StartedOn { get; private set; }
    public DateTimeOffset? CompletedOn { get; private set; }
    public string? RunBy { get; private set; }
    public string? FailureReason { get; private set; }
    public string? Description { get; private set; }

    private readonly List<MaterialRequirementPlanLine> _lines = [];
    public IReadOnlyList<MaterialRequirementPlanLine> Lines => _lines.AsReadOnly();

    private MaterialRequirementPlan() { }

    public MaterialRequirementPlan(string code, DateRange horizon, string? runBy = null)
    {
        ArgumentNullException.ThrowIfNull(horizon, nameof(horizon));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        if (horizon.EndDate is null)
            throw new ArgumentException("A planning horizon must be closed at both ends.", nameof(horizon));

        Code = code.Trim();
        Horizon = horizon;
        RunBy = runBy;
        StartedOn = DateTimeOffset.UtcNow;
        Status = MaterialRequirementPlanStatus.Running;
    }

    public void BaseOnForecast(Guid demandForecastId) =>
        DemandForecastId = demandForecastId == Guid.Empty ? null : demandForecastId;

    public void ScopeToWarehouse(Guid warehouseId) => WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddLine(MaterialRequirementPlanLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        EnsureCanAddChild(line, line.PlanId, _lines);

        if (Status != MaterialRequirementPlanStatus.Running)
            throw new InvalidOperationException($"Lines can only be written while the run is in progress; this one is {Status}.");
        _lines.Add(line);
    }

    public void Complete()
    {
        if (Status != MaterialRequirementPlanStatus.Running)
            throw new InvalidOperationException($"A run in state {Status} cannot be completed.");
        Status = MaterialRequirementPlanStatus.Completed;
        CompletedOn = DateTimeOffset.UtcNow;
        Raise(new MaterialRequirementPlanCompleted(Id, Code, ShortageLines.Count));
    }

    /// <summary>
    /// Ends a run that could not finish. The lines written so far are kept, because a half finished
    /// run is usually the fastest way to see which item broke it.
    /// </summary>
    public void Fail(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != MaterialRequirementPlanStatus.Running)
            throw new InvalidOperationException($"A run in state {Status} cannot fail.");
        Status = MaterialRequirementPlanStatus.Failed;
        CompletedOn = DateTimeOffset.UtcNow;
        FailureReason = reason.Trim();
    }

    /// <summary>
    /// Marks the run as no longer worth acting on. Released lines keep whatever they turned into;
    /// only the unreleased suggestions become void.
    /// </summary>
    public void Supersede()
    {
        if (Status != MaterialRequirementPlanStatus.Completed)
            throw new InvalidOperationException("Only a completed run can be superseded.");
        Status = MaterialRequirementPlanStatus.Superseded;
    }

    /// <summary>Lines that need somebody to do something, soonest first.</summary>
    public IReadOnlyList<MaterialRequirementPlanLine> ShortageLines =>
        [.. _lines.Where(l => l.NeedsAction).OrderBy(l => l.RequiredOn)];

    public IReadOnlyList<MaterialRequirementPlanLine> PendingReleaseLines =>
        [.. _lines.Where(l => l.NeedsAction && !l.IsReleased).OrderBy(l => l.RequiredOn)];

    public bool IsActionable => Status == MaterialRequirementPlanStatus.Completed;
}
