namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A unit things are counted or measured in - واحد اندازه‌گیری.
/// </summary>
public sealed class UnitOfMeasure : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private readonly List<ItemUnitOfMeasure> _itemGroups = [];
    public IReadOnlyList<ItemUnitOfMeasure> ItemGroups => _itemGroups.AsReadOnly();

    private UnitOfMeasure() { }

    public UnitOfMeasure(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
    }

    public void Update(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
}
