namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One line of a count sheet: what the books said, what was found, and the difference between them.
/// </summary>
public sealed class StockCountItem : BaseEntity
{
    public StockCount Count { get; private set; } = default!;
    public Guid CountId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public Guid? ItemBatchId { get; private set; }
    public Guid? StorageLocationId { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>What the ledger held when the count was frozen.</summary>
    public decimal SystemQuantity { get; private set; }

    public decimal? CountedQuantity { get; private set; }

    /// <summary>A second count, requested when the first disagreed with the books.</summary>
    public decimal? RecountedQuantity { get; private set; }

    public Money UnitCost { get; private set; } = default!;

    public decimal Variance => (CountedQuantity ?? SystemQuantity) - SystemQuantity;
    public Money VarianceValue => UnitCost.Multiply(Variance);
    public bool HasDiscrepancy => CountedQuantity.HasValue && Variance != 0m;
    public bool IsCounted => CountedQuantity.HasValue;

    public string? DiscrepancyReason { get; private set; }

    private StockCountItem() { }

    public StockCountItem(StockCount count, Item item, decimal systemQuantity, Guid unitOfMeasureId, Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(count, nameof(count));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        ArgumentOutOfRangeException.ThrowIfNegative(systemQuantity, nameof(systemQuantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        Count = count;
        CountId = count.Id;
        Item = item;
        ItemId = item.Id;
        SystemQuantity = systemQuantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitCost = unitCost;
    }

    public void SetBatch(Guid itemBatchId) => ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;

    public void ScopeToStorageLocation(Guid storageLocationId) =>
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;

    public void RecordCount(decimal countedQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(countedQuantity, nameof(countedQuantity));
        CountedQuantity = countedQuantity;
    }

    public void RecordRecount(decimal recountedQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(recountedQuantity, nameof(recountedQuantity));
        if (CountedQuantity is null)
            throw new InvalidOperationException("A first count must be recorded before a recount.");
        RecountedQuantity = recountedQuantity;
        CountedQuantity = recountedQuantity;
    }

    public void ExplainDiscrepancy(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        DiscrepancyReason = reason.Trim();
    }
}
