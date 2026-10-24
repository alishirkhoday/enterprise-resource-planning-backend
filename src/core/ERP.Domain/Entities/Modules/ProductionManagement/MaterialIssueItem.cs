using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// One component taken into a works order: what the recipe called for and what was actually
/// used. The gap between the two is where material loss shows up.
/// </summary>
public sealed class MaterialIssueItem : BaseEntity
{
    public MaterialIssue Issue { get; private set; } = default!;
    public Guid IssueId { get; private set; }

    public Item Component { get; private set; } = default!;
    public Guid ComponentId { get; private set; }

    public short LineNumber { get; private set; }

    /// <summary>What the bill of materials called for.</summary>
    public decimal PlannedQuantity { get; private set; }

    /// <summary>What was actually taken from the warehouse.</summary>
    public decimal IssuedQuantity { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitCost { get; private set; } = default!;
    public Money TotalCost => UnitCost.Multiply(IssuedQuantity);

    public Guid? ItemBatchId { get; private set; }
    public Guid? SerialNumberId { get; private set; }
    public Guid? StorageLocationId { get; private set; }
    public string? Description { get; private set; }

    /// <summary>How much more, or less, was used than planned.</summary>
    public decimal Variance => IssuedQuantity - PlannedQuantity;

    public Money VarianceValue => UnitCost.Multiply(Variance);

    private MaterialIssueItem() { }

    public MaterialIssueItem(MaterialIssue issue, Item component, short lineNumber, decimal plannedQuantity, decimal issuedQuantity, Guid unitOfMeasureId, Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(issue, nameof(issue));
        ArgumentNullException.ThrowIfNull(component, nameof(component));
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        ArgumentOutOfRangeException.ThrowIfNegative(plannedQuantity, nameof(plannedQuantity));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(issuedQuantity, nameof(issuedQuantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        Issue = issue;
        IssueId = issue.Id;
        Component = component;
        ComponentId = component.Id;
        LineNumber = lineNumber;
        PlannedQuantity = plannedQuantity;
        IssuedQuantity = issuedQuantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitCost = unitCost;
    }

    internal void SetTracking(Guid? itemBatchId, Guid? serialNumberId)
    {
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
    }

    internal void ScopeToStorageLocation(Guid storageLocationId) =>
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
