using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.WarehouseManagement;

public sealed partial class WarehouseTransaction
{
    public void SetWarehouseTransactionItemTracking(Guid childId, Guid? itemBatchId, Guid? serialNumberId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTracking(itemBatchId, serialNumberId);
    }

    public void SetWarehouseTransactionItemStorageLocations(Guid childId, Guid? sourceStorageLocationId, Guid? destinationStorageLocationId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetStorageLocations(sourceStorageLocationId, destinationStorageLocationId);
    }

    public void SetWarehouseTransactionItemUnitCost(Guid childId, Money unitCost)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetUnitCost(unitCost);
    }

    public void SetWarehouseTransactionItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }
}
