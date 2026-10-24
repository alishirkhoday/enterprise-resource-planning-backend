namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class PurchaseReturn
{
    public void SetPurchaseReturnItemTracking(Guid childId, Guid? itemBatchId, Guid? serialNumberId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTracking(itemBatchId, serialNumberId);
    }

    public void SetPurchaseReturnItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }
}
