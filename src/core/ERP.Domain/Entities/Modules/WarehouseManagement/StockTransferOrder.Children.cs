using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.WarehouseManagement;

public sealed partial class StockTransferOrder
{
    public void SetStockTransferOrderItemBatch(Guid childId, Guid itemBatchId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetBatch(itemBatchId);
    }

    public void SetStockTransferOrderItemStorageLocations(Guid childId, Guid? sourceStorageLocationId, Guid? destinationStorageLocationId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetStorageLocations(sourceStorageLocationId, destinationStorageLocationId);
    }

    public void SetStockTransferOrderItemUnitCost(Guid childId, Money unitCost)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetUnitCost(unitCost);
    }

    public void DispatchStockTransferOrderItem(Guid childId, decimal quantity)
    {
        if (Status != StockTransferStatus.Approved)
            throw new InvalidOperationException("Only an approved transfer can dispatch goods.");
        var child = GetChild(childId, _items);
        child.Dispatch(quantity);
    }

    public void ReceiveStockTransferOrderItem(Guid childId, decimal quantity)
    {
        if (Status != StockTransferStatus.InTransit)
            throw new InvalidOperationException("Only a transfer in transit can receive goods.");
        var child = GetChild(childId, _items);
        child.Receive(quantity);
    }

    public void ExplainStockTransferOrderItemDiscrepancy(Guid childId, string reason)
    {
        var child = GetChild(childId, _items);
        child.ExplainDiscrepancy(reason);
    }
    public void DispatchStockTransferOrderItem(Guid childId, decimal quantity, Money unitCost)
    {
        if (Status != StockTransferStatus.Approved)
            throw new InvalidOperationException("Only an approved transfer can dispatch goods.");
        var child = GetChild(childId, _items);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (quantity > child.RequestedQuantity)
            throw new InvalidOperationException("Dispatch quantity exceeds the requested quantity.");
        child.SetUnitCost(unitCost);
        child.Dispatch(quantity);
    }
}
