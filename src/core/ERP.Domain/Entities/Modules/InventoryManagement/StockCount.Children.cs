namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class StockCount
{
    public void SetStockCountItemBatch(Guid childId, Guid itemBatchId)
    {
        var child = GetChild(childId, _items);
        child.SetBatch(itemBatchId);
    }

    public void ScopeStockCountItemToStorageLocation(Guid childId, Guid storageLocationId)
    {
        var child = GetChild(childId, _items);
        child.ScopeToStorageLocation(storageLocationId);
    }

    public void RecordStockCountItemCount(Guid childId, decimal countedQuantity)
    {
        var child = GetChild(childId, _items);
        child.RecordCount(countedQuantity);
    }

    public void RecordStockCountItemRecount(Guid childId, decimal recountedQuantity)
    {
        var child = GetChild(childId, _items);
        child.RecordRecount(recountedQuantity);
    }

    public void ExplainStockCountItemDiscrepancy(Guid childId, string reason)
    {
        var child = GetChild(childId, _items);
        child.ExplainDiscrepancy(reason);
    }
}
