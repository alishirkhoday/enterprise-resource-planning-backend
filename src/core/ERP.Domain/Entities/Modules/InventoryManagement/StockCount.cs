namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A physical count of what is actually in a warehouse, and the adjustments needed to make the
/// books agree with it.
/// </summary>
public sealed partial class StockCount : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;
    public Guid WarehouseId { get; private set; }
    public Guid? StorageLocationId { get; private set; }

    public StockCountType Type { get; private set; }
    public StockCountStatus Status { get; private set; }

    public DateOnly CountDate { get; private set; }
    public DateTimeOffset? FrozenOn { get; private set; }
    public DateTimeOffset? CompletedOn { get; private set; }

    public Guid? CountedByHumanId { get; private set; }
    public Guid? ApprovedByHumanId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? AdjustmentDocumentId { get; private set; }
    public string? Description { get; private set; }

    private readonly List<StockCountItem> _items = [];
    public IReadOnlyList<StockCountItem> Items => _items.AsReadOnly();

    private StockCount() { }

    public StockCount(string number, Guid warehouseId, StockCountType type, DateOnly countDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is empty.", nameof(warehouseId));

        Number = number.Trim();
        WarehouseId = warehouseId;
        Type = type;
        CountDate = countDate;
        Status = StockCountStatus.Planned;
    }

    public void ScopeToStorageLocation(Guid storageLocationId) =>
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;

    public void AssignCounter(Guid humanId) => CountedByHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(StockCountItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.CountId, _items);

        if (Status is not (StockCountStatus.Planned or StockCountStatus.InProgress))
            throw new InvalidOperationException($"Lines cannot be added while the count is {Status}.");
        if (_items.Any(i => i.ItemId == item.ItemId && i.ItemBatchId == item.ItemBatchId))
            throw new InvalidOperationException("That item is already on the count sheet.");
        _items.Add(item);
    }

    /// <summary>
    /// Freezes the book quantities so the count is measured against a fixed snapshot rather than
    /// a balance that keeps moving while people are counting.
    /// </summary>
    public void Freeze()
    {
        if (Status != StockCountStatus.Planned)
            throw new InvalidOperationException($"Only a planned count can be frozen; this one is {Status}.");
        if (_items.Count == 0)
            throw new InvalidOperationException("A count cannot be frozen without any lines.");
        Status = StockCountStatus.InProgress;
        FrozenOn = DateTimeOffset.UtcNow;
    }

    public void SubmitForReview()
    {
        if (Status != StockCountStatus.InProgress)
            throw new InvalidOperationException($"Only a count in progress can be submitted; this one is {Status}.");
        if (_items.Any(i => !i.IsCounted))
            throw new InvalidOperationException("Every line must be counted before the sheet is submitted.");
        Status = StockCountStatus.AwaitingApproval;
    }

    /// <summary>
    /// Approves the count. The caller is expected to write the matching adjustment entries to the
    /// stock ledger in the same transaction.
    /// </summary>
    public void Approve(Guid approvedByHumanId, Guid? adjustmentDocumentId = null)
    {
        if (Status != StockCountStatus.AwaitingApproval)
            throw new InvalidOperationException($"Only a submitted count can be approved; this one is {Status}.");
        ApprovedByHumanId = approvedByHumanId;
        AdjustmentDocumentId = adjustmentDocumentId == Guid.Empty ? null : adjustmentDocumentId;
        Status = StockCountStatus.Completed;
        CompletedOn = DateTimeOffset.UtcNow;

        foreach (var item in _items.Where(i => i.HasDiscrepancy))
        {
            Raise(new StockCountDiscrepancyFound(Id, item.ItemId, item.Variance));
        }
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == StockCountStatus.Completed)
            throw new InvalidOperationException("A completed count cannot be cancelled.");
        Status = StockCountStatus.Cancelled;
        Description = reason.Trim();
    }

    public IEnumerable<StockCountItem> Discrepancies => _items.Where(i => i.HasDiscrepancy);

    public decimal TotalSurplus => _items.Where(i => i.Variance > 0m).Sum(i => i.Variance);
    public decimal TotalShortage => _items.Where(i => i.Variance < 0m).Sum(i => Math.Abs(i.Variance));
}
