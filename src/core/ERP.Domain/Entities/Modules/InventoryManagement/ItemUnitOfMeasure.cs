namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A unit an item can be handled in, beyond its base unit - a box, a pallet.
/// </summary>
public sealed class ItemUnitOfMeasure : BaseEntity
{
    public ItemGroup ItemGroup { get; private set; } = default!;
    public Guid ItemGroupId { get; private set; }
    public UnitOfMeasure UnitOfMeasure { get; private set; } = default!;
    public Guid UnitOfMeasureId { get; private set; }

    private ItemUnitOfMeasure() { }

    public ItemUnitOfMeasure(ItemGroup itemGroup, UnitOfMeasure unitOfMeasure)
    {
        ArgumentNullException.ThrowIfNull(itemGroup, nameof(itemGroup));
        ArgumentNullException.ThrowIfNull(unitOfMeasure, nameof(unitOfMeasure));

        ItemGroup = itemGroup;
        ItemGroupId = itemGroup.Id;
        UnitOfMeasure = unitOfMeasure;
        UnitOfMeasureId = unitOfMeasure.Id;
    }
}
