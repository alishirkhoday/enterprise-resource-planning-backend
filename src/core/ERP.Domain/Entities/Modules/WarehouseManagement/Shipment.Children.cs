using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.WarehouseManagement;

public sealed partial class Shipment
{
    public void LinkShipmentItemToSaleOrderItem(Guid childId, Guid saleOrderItemId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.LinkToSaleOrderItem(saleOrderItemId);
    }

    public void SetShipmentItemTracking(Guid childId, Guid? itemBatchId, Guid? serialNumberId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTracking(itemBatchId, serialNumberId);
    }

    public void ScopeShipmentItemToStorageLocation(Guid childId, Guid storageLocationId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.ScopeToStorageLocation(storageLocationId);
    }

    public void SetShipmentItemPackageReference(Guid childId, string? packageReference)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetPackageReference(packageReference);
    }
}
