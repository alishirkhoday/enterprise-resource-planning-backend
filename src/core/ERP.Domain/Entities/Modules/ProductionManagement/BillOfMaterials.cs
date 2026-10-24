using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// What one item is made of, for a stated output quantity. Versioning it is what lets an old
/// production order still be explained after the recipe changes.
/// </summary>
public sealed partial class BillOfMaterials : AggregateRoot
{
    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public string Code { get; private set; } = default!;
    public int Version { get; private set; }
    public BillOfMaterialsStatus Status { get; private set; }

    /// <summary>How many units of the item the component quantities produce.</summary>
    public decimal OutputQuantity { get; private set; }
    public Guid? OutputUnitOfMeasureId { get; private set; }

    public DateRange ValidityPeriod { get; private set; } = default!;
    public bool IsDefault { get; private set; }
    public Guid? RoutingId { get; private set; }

    public string Description { get; private set; } = default!;

    private readonly List<BillOfMaterialItem> _components = [];
    public IReadOnlyList<BillOfMaterialItem> Components => _components.AsReadOnly();

    private BillOfMaterials() { }

    public BillOfMaterials(Item item, string code, string description, DateRange validityPeriod, decimal outputQuantity = 1m)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(validityPeriod, nameof(validityPeriod));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outputQuantity, nameof(outputQuantity));

        Item = item;
        ItemId = item.Id;
        Code = code.Trim();
        Description = description.Trim();
        ValidityPeriod = validityPeriod;
        OutputQuantity = outputQuantity;
        Version = 1;
        Status = BillOfMaterialsStatus.Draft;
    }

    public void SetOutput(decimal outputQuantity, Guid? unitOfMeasureId)
    {
        EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outputQuantity, nameof(outputQuantity));
        OutputQuantity = outputQuantity;
        OutputUnitOfMeasureId = unitOfMeasureId == Guid.Empty ? null : unitOfMeasureId;
    }

    public void LinkRouting(Guid routingId) => RoutingId = routingId == Guid.Empty ? null : routingId;

    public void AddComponent(BillOfMaterialItem component)
    {
        ArgumentNullException.ThrowIfNull(component);
        EnsureCanAddChild(component, component.BillOfMaterialsId, _components);

        EnsureDraft();
        if (component.ComponentId == ItemId)
            throw new InvalidOperationException("An item cannot be a component of itself.");
        if (_components.Any(c => c.ComponentId == component.ComponentId && c.AlternativeGroup == component.AlternativeGroup))
            throw new InvalidOperationException("That component is already on this bill of materials.");
        _components.Add(component);
    }

    public void RemoveComponent(BillOfMaterialItem component)
    {
        ArgumentNullException.ThrowIfNull(component);
        if (component.BillOfMaterialsId != Id || !_components.Contains(component))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(component, nameof(component));
        EnsureDraft();
        _components.Remove(component);
    }

    public void Approve()
    {
        EnsureDraft();
        if (_components.Count == 0)
            throw new InvalidOperationException("A bill of materials cannot be approved without components.");
        Status = BillOfMaterialsStatus.Approved;
    }

    public void Revise()
    {
        if (Status != BillOfMaterialsStatus.Approved)
            throw new InvalidOperationException("Only an approved bill of materials can be revised.");
        Version++;
        Status = BillOfMaterialsStatus.Draft;
    }

    public void Obsolete()
    {
        Status = BillOfMaterialsStatus.Obsolete;
        IsDefault = false;
    }

    public void MarkAsDefault()
    {
        if (Status != BillOfMaterialsStatus.Approved)
            throw new InvalidOperationException("Only an approved bill of materials can be made the default.");
        IsDefault = true;
    }

    /// <summary>
    /// Scales the components up to the quantity actually being made, including expected scrap.
    /// </summary>
    public IReadOnlyList<(Guid ComponentId, decimal Quantity, Guid UnitOfMeasureId)> Explode(decimal producedQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(producedQuantity, nameof(producedQuantity));
        var factor = producedQuantity / OutputQuantity;
        return _components
            .Where(c => !c.IsAlternative)
            .Select(c => (c.ComponentId, c.RequiredQuantityFor(factor), c.UnitOfMeasureId))
            .ToList();
    }

    public bool IsEffectiveOn(DateOnly date) => Status == BillOfMaterialsStatus.Approved && ValidityPeriod.Contains(date);

    private void EnsureDraft()
    {
        if (Status != BillOfMaterialsStatus.Draft)
            throw new InvalidOperationException($"The bill of materials is {Status} and can no longer be edited; revise it instead.");
    }
}
