using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

public sealed partial class BillOfMaterials
{
    public void SetBillOfMaterialItemScrapPercentage(Guid childId, Percentage scrapPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _components);
        child.SetScrapPercentage(scrapPercentage);
    }

    public void ConsumeBillOfMaterialItemAtOperation(Guid childId, int operationSequenceNumber)
    {
        var child = GetChild(childId, _components);
        child.ConsumeAtOperation(operationSequenceNumber);
    }

    public void MarkBillOfMaterialItemAsAlternative(Guid childId, string alternativeGroup)
    {
        var child = GetChild(childId, _components);
        child.MarkAsAlternative(alternativeGroup);
    }

    public void MarkBillOfMaterialItemAsPrimary(Guid childId, string alternativeGroup)
    {
        var child = GetChild(childId, _components);
        child.MarkAsPrimary(alternativeGroup);
    }

    public void SetBillOfMaterialItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _components);
        child.SetDescription(description);
    }
}
