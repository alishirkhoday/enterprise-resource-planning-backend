namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// What sales is expected to need over a horizon, per item and per bucket. It is the one input to
/// planning that is not a fact: everything else an <see cref="MaterialRequirementPlan"/> reads is
/// already recorded somewhere, while this is a judgement that will turn out wrong by some margin.
/// <para>
/// Keeping it as its own document rather than as numbers inside a plan means a forecast can be
/// argued over, revised and compared against what actually happened.
/// </para>
/// </summary>
public sealed partial class DemandForecast : AggregateRoot, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    public DateRange Horizon { get; private set; } = default!;
    public ForecastBucket Bucket { get; private set; }
    public ForecastMethod Method { get; private set; }
    public DemandForecastStatus Status { get; private set; }

    public Guid? WarehouseId { get; private set; }
    public Guid? BranchId { get; private set; }

    public DateTimeOffset? ApprovedOn { get; private set; }
    public string? ApprovedBy { get; private set; }
    public string? Description { get; private set; }

    private readonly List<DemandForecastLine> _lines = [];
    public IReadOnlyList<DemandForecastLine> Lines => _lines.AsReadOnly();

    private DemandForecast() { }

    public DemandForecast(string code, string name, DateRange horizon, ForecastBucket bucket, ForecastMethod method)
    {
        ArgumentNullException.ThrowIfNull(horizon, nameof(horizon));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        if (horizon.EndDate is null)
            throw new ArgumentException("A forecast horizon must be closed at both ends.", nameof(horizon));

        Code = code.Trim();
        Name = name.Trim();
        Horizon = horizon;
        Bucket = bucket;
        Method = method;
        Status = DemandForecastStatus.Draft;
    }

    public void ScopeToWarehouse(Guid warehouseId) => WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddLine(DemandForecastLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        EnsureCanAddChild(line, line.ForecastId, _lines);

        EnsureDraft();
        if (!Horizon.Contains(line.BucketStartDate))
            throw new InvalidOperationException($"The bucket starting {line.BucketStartDate} falls outside the forecast horizon.");
        if (_lines.Any(l => l.ItemId == line.ItemId && l.BucketStartDate == line.BucketStartDate))
            throw new InvalidOperationException("This item already has a figure for that bucket.");
        _lines.Add(line);
    }

    public void RemoveLine(DemandForecastLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (line.ForecastId != Id || !_lines.Contains(line))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(line, nameof(line));
        EnsureDraft();
        _lines.Remove(line);
    }

    public void Approve(string? approvedBy = null)
    {
        EnsureDraft();
        if (_lines.Count == 0)
            throw new InvalidOperationException("An empty forecast cannot be approved.");
        Status = DemandForecastStatus.Approved;
        ApprovedOn = DateTimeOffset.UtcNow;
        ApprovedBy = approvedBy;
    }

    /// <summary>
    /// Takes the forecast out of use without deleting it, so the plans that were built on it still
    /// explain themselves.
    /// </summary>
    public void Supersede()
    {
        if (Status != DemandForecastStatus.Approved)
            throw new InvalidOperationException("Only an approved forecast can be superseded.");
        Status = DemandForecastStatus.Superseded;
    }

    public decimal ForecastFor(Guid itemId, DateOnly onDate) =>
        _lines.Where(l => l.ItemId == itemId && l.Covers(onDate)).Sum(l => l.Quantity);

    public decimal TotalFor(Guid itemId) => _lines.Where(l => l.ItemId == itemId).Sum(l => l.Quantity);

    public bool IsUsable => Status == DemandForecastStatus.Approved;

    private void EnsureDraft()
    {
        if (Status != DemandForecastStatus.Draft)
            throw new InvalidOperationException($"A forecast in state {Status} can no longer be edited.");
    }
}
