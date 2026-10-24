using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.OrderManagement;

public sealed partial class ReturnRequest
{
    public void InspectReturnRequestItem(Guid childId, decimal acceptedQuantity, ReturnItemCondition condition, string? notes = null)
    {
        var child = GetChild(childId, _items);
        EnsureOpen();
        child.Inspect(acceptedQuantity, condition, notes);
    }

    public void SetReturnRequestItemTracking(Guid childId, Guid? itemBatchId, Guid? serialNumberId)
    {
        EnsureOpen();
        var child = GetChild(childId, _items);
        child.SetTracking(itemBatchId, serialNumberId);
    }
}
