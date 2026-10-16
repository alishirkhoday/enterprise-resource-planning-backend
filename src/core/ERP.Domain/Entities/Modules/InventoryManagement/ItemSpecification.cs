namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A technical detail of an item, for a datasheet or a tender. Descriptive rather than
/// something the system reasons about.
/// </summary>
public sealed class ItemSpecification : BaseEntity
{
    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }
    public ItemAttribute Attribute { get; private set; } = default!;
    public Guid AttributeId { get; private set; }
    public ItemAttributeValue Value { get; private set; } = default!;
    public Guid ValueId { get; private set; }

    private ItemSpecification() { }

    public ItemSpecification(Item item, ItemAttribute attribute, ItemAttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(attribute, nameof(attribute));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        if (value.AttributeId != attribute.Id)
            throw new ArgumentException("Value does not belong to the given Attribute.", nameof(value));

        Item = item;
        ItemId = item.Id;
        Attribute = attribute;
        AttributeId = attribute.Id;
        Value = value;
        ValueId = value.Id;
    }
}
