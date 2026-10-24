namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One value an attribute can take, such as "red" for colour.
/// </summary>
public sealed class ItemAttributeValue : AggregateRoot
{
    public ItemAttribute Attribute { get; private set; } = default!;
    public Guid AttributeId { get; private set; }
    public string Value { get; private set; } = default!;
    public string? Description { get; private set; }

    private readonly List<ItemSpecification> _specifications = [];
    public IReadOnlyList<ItemSpecification> Specifications => _specifications.AsReadOnly();

    private ItemAttributeValue() { }

    public ItemAttributeValue(ItemAttribute attribute, string value)
    {
        ArgumentNullException.ThrowIfNull(attribute, nameof(attribute));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        Attribute = attribute;
        AttributeId = attribute.Id;
        Value = value.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
}
