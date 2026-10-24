namespace ERP.Domain.Entities.Modules.WarehouseManagement;

public sealed partial class Warehouse
{
    public void UpdateWarehouseStorageLocation(Guid childId, string code, string name)
    {
        var child = GetChild(childId, _storageLocations);
        child.Update(code, name);
    }

    public void SetWarehouseStorageLocationDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _storageLocations);
        child.SetDescription(description);
    }
}
