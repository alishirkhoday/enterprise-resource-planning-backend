namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A family of items - گروه کالا. What reports subtotal by and what a targeted price list or
/// sales target can be narrowed to.
/// </summary>
public sealed class ItemGroup : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsSystematic { get; private set; }
    public bool IsActive { get; private set; }
    public string? Description { get; private set; }

    private readonly List<ItemAttribute> _attributes = [];
    public IReadOnlyList<ItemAttribute> Attributes => _attributes.AsReadOnly();

    private readonly List<Item> _items = [];
    public IReadOnlyList<Item> Items => _items.AsReadOnly();

    private readonly List<ItemUnitOfMeasure> _unitOfMeasures = [];
    public IReadOnlyList<ItemUnitOfMeasure> UnitOfMeasures => _unitOfMeasures.AsReadOnly();

    private ItemGroup() { }

    public ItemGroup(string code, string name, bool isSystematic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        IsSystematic = isSystematic;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
    public void AddUnitOfMeasure(ItemUnitOfMeasure child)
    {
        ArgumentNullException.ThrowIfNull(child);
        EnsureCanAddChild(child, child.ItemGroupId, _unitOfMeasures);
        _unitOfMeasures.Add(child);
    }
}
