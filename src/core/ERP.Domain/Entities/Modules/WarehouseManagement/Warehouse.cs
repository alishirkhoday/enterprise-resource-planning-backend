namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// A place stock is held - انبار. Every quantity in the system is a quantity somewhere, which
/// is why almost nothing about inventory can be asked without naming one.
/// </summary>
public sealed class Warehouse : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public WarehouseType Type { get; private set; }
    public Address Address { get; private set; } = default!;
    public string? Description { get; private set; }

    private readonly List<WarehouseStorageLocation> _storageLocations = [];
    public IReadOnlyList<WarehouseStorageLocation> StorageLocations => _storageLocations.AsReadOnly();

    private Warehouse() { }

    public Warehouse(string code, string name, WarehouseType type, Address address, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        Address = address;
        Description = description;
    }

    public void Update(string code, string name, WarehouseType type, Address address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        Address = address;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();

    public void AddStorageLocation(WarehouseStorageLocation storageLocation)
    {
        ArgumentNullException.ThrowIfNull(storageLocation, nameof(storageLocation));

        if (_storageLocations.Any(l => l.Code.Equals(storageLocation.Code, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Storage location with code '{storageLocation.Code}' already exists in this warehouse.", nameof(storageLocation));

        _storageLocations.Add(storageLocation);
    }

    public void RemoveStorageLocation(WarehouseStorageLocation storageLocation)
    {
        ArgumentNullException.ThrowIfNull(storageLocation, nameof(storageLocation));
        _storageLocations.Remove(storageLocation);
    }
}
