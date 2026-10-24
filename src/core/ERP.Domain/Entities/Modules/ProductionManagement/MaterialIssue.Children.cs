using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

public sealed partial class MaterialIssue
{
    public void SetMaterialIssueItemTracking(Guid childId, Guid? itemBatchId, Guid? serialNumberId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTracking(itemBatchId, serialNumberId);
    }

    public void ScopeMaterialIssueItemToStorageLocation(Guid childId, Guid storageLocationId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.ScopeToStorageLocation(storageLocationId);
    }

    public void SetMaterialIssueItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }
}
