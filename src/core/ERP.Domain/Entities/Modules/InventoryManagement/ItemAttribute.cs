namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A property items can vary by - colour, size. What variants are generated from.
/// </summary>
public sealed class ItemAttribute : BaseEntity
{
    public ItemGroup Group { get; private set; } = default!;
    public Guid GroupId { get; private set; }
    public string Title { get; private set; } = default!;
    public bool IsBoolean { get; private set; }
    public string? Description { get; private set; }

    private readonly List<ItemAttributeValue> _values = [];
    public IReadOnlyList<ItemAttributeValue> Values => _values;

    private readonly List<ItemSpecification> _specifications = [];
    public IReadOnlyList<ItemSpecification> Specifications => _specifications;

    private ItemAttribute() { }

    public ItemAttribute(ItemGroup group, string title, bool isBoolean)
    {
        ArgumentNullException.ThrowIfNull(group, nameof(group));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Group = group;
        GroupId = group.Id;
        Title = title.Trim();
        IsBoolean = isBoolean;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
}
