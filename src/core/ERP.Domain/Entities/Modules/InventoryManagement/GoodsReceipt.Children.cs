namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class GoodsReceipt
{
    public void RejectGoodsReceiptItem(Guid childId, decimal rejectedQuantity, string reason)
    {
        var child = GetChild(childId, _items);
        child.Reject(rejectedQuantity, reason);
    }

    public void SetGoodsReceiptItemBatch(Guid childId, Guid itemBatchId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetBatch(itemBatchId);
    }

    public void AddGoodsReceiptItemSerialNumber(Guid childId, Guid serialNumberId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.AddSerialNumber(serialNumberId);
    }

    public void ScopeGoodsReceiptItemToStorageLocation(Guid childId, Guid storageLocationId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.ScopeToStorageLocation(storageLocationId);
    }

    public void MarkGoodsReceiptItemAsRequiringInspection(Guid childId)
    {
        var child = GetChild(childId, _items);
        child.MarkAsRequiringInspection();
    }
}
