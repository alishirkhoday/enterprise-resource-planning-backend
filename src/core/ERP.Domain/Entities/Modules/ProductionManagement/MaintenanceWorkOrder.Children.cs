using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

public sealed partial class MaintenanceWorkOrder
{
    public void SetMaintenancePartUsageSource(Guid childId, Guid? warehouseId, Guid? itemBatchId)
    {
        var child = GetChild(childId, _partsUsed);
        child.SetSource(warehouseId, itemBatchId);
    }

    public void SetMaintenancePartUsageDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _partsUsed);
        child.SetDescription(description);
    }
}
