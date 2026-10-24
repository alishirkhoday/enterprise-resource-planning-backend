using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

public sealed partial class ServiceOrder
{
    public void SetServiceOrderPartTracking(Guid childId, Guid? itemBatchId, Guid? serialNumberId)
    {
        EnsureOpen();
        var child = GetChild(childId, _parts);
        child.SetTracking(itemBatchId, serialNumberId);
    }

    public void SetServiceOrderPartUnitCost(Guid childId, Money unitCost)
    {
        EnsureOpen();
        var child = GetChild(childId, _parts);
        child.SetUnitCost(unitCost);
    }

    public void CoverServiceOrderPartUnderWarranty(Guid childId)
    {
        EnsureOpen();
        var child = GetChild(childId, _parts);
        child.CoverUnderWarranty();
    }

    public void MakeServiceOrderPartChargeable(Guid childId)
    {
        EnsureOpen();
        var child = GetChild(childId, _parts);
        child.MakeChargeable();
    }

    public void MarkServiceOrderPartAsReturnedForInspection(Guid childId)
    {
        var child = GetChild(childId, _parts);
        child.MarkAsReturnedForInspection();
    }

    public void SetServiceOrderPartDescription(Guid childId, string? description)
    {
        EnsureOpen();
        var child = GetChild(childId, _parts);
        child.SetDescription(description);
    }
}
