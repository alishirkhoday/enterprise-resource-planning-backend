namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class PurchaseRequisition
{
    public void SetPurchaseRequisitionItemEstimatedUnitPrice(Guid childId, Money estimatedUnitPrice)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetEstimatedUnitPrice(estimatedUnitPrice);
    }

    public void SuggestPurchaseRequisitionItemSupplier(Guid childId, Guid supplierId)
    {
        var child = GetChild(childId, _items);
        child.SuggestSupplier(supplierId);
    }

    public void SetPurchaseRequisitionItemRequiredByDate(Guid childId, DateOnly requiredByDate)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetRequiredByDate(requiredByDate);
    }

    public void SetPurchaseRequisitionItemSpecification(Guid childId, string? specification)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetSpecification(specification);
    }

    public void RecordPurchaseRequisitionItemOrdered(Guid childId, decimal quantity)
    {
        var child = GetChild(childId, _items);
        child.RecordOrdered(quantity);
    }
}
