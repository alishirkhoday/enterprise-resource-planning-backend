namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// A place inside a warehouse - راهرو، قفسه. Optional: a small warehouse tracks stock at the
/// building, a large one needs to know the shelf.
/// </summary>
public sealed class WarehouseStorageLocation : BaseEntity
{
    public Warehouse Warehouse { get; private set; } = default!;
    public Guid WarehouseId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private WarehouseStorageLocation() { }

    public WarehouseStorageLocation(Warehouse warehouse, string code, string name, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(warehouse, nameof(warehouse));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Warehouse = warehouse;
        WarehouseId = warehouse.Id;
        Code = code.Trim();
        Name = name.Trim();
        Description = description;
    }

    public void Update(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
}
