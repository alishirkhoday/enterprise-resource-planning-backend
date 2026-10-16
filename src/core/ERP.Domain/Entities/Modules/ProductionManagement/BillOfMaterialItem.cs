using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// One component of a bill of materials, with the loss expected when it is used.
/// </summary>
public sealed class BillOfMaterialItem : BaseEntity
{
    public BillOfMaterials BillOfMaterials { get; private set; } = default!;
    public Guid BillOfMaterialsId { get; private set; }

    public Item Component { get; private set; } = default!;
    public Guid ComponentId { get; private set; }

    public short LineNumber { get; private set; }

    /// <summary>How much is needed to make the bill output quantity.</summary>
    public decimal Quantity { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>Expected loss of this component, which inflates what is actually issued.</summary>
    public Percentage ScrapPercentage { get; private set; } = default!;

    /// <summary>The routing step that consumes it, so material is issued when it is needed.</summary>
    public int? OperationSequenceNumber { get; private set; }

    /// <summary>
    /// Groups interchangeable components. Members of a group with <see cref="IsAlternative"/> set
    /// stand in for the primary one when it is unavailable.
    /// </summary>
    public string? AlternativeGroup { get; private set; }
    public bool IsAlternative { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Kept for the older accessor that expressed the unit as free text.</summary>
    public string Unit { get; private set; } = default!;

    private BillOfMaterialItem() { }

    public BillOfMaterialItem(BillOfMaterials billOfMaterials, Item component, short lineNumber, decimal quantity, Guid unitOfMeasureId, string unit)
    {
        ArgumentNullException.ThrowIfNull(billOfMaterials, nameof(billOfMaterials));
        ArgumentNullException.ThrowIfNull(component, nameof(component));
        ArgumentException.ThrowIfNullOrWhiteSpace(unit, nameof(unit));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        BillOfMaterials = billOfMaterials;
        BillOfMaterialsId = billOfMaterials.Id;
        Component = component;
        ComponentId = component.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        Unit = unit.Trim();
        ScrapPercentage = Percentage.Zero();
    }

    public void SetScrapPercentage(Percentage scrapPercentage)
    {
        ArgumentNullException.ThrowIfNull(scrapPercentage, nameof(scrapPercentage));
        ScrapPercentage = scrapPercentage;
    }

    public void ConsumeAtOperation(int operationSequenceNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(operationSequenceNumber, nameof(operationSequenceNumber));
        OperationSequenceNumber = operationSequenceNumber;
    }

    public void MarkAsAlternative(string alternativeGroup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alternativeGroup, nameof(alternativeGroup));
        AlternativeGroup = alternativeGroup.Trim();
        IsAlternative = true;
    }

    public void MarkAsPrimary(string alternativeGroup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alternativeGroup, nameof(alternativeGroup));
        AlternativeGroup = alternativeGroup.Trim();
        IsAlternative = false;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>How much to issue for a scale factor, including the expected scrap.</summary>
    public decimal RequiredQuantityFor(decimal factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor, nameof(factor));
        return Quantity * factor * (1m + ScrapPercentage.AsFraction());
    }
}
