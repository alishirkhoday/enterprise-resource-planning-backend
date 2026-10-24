namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class PurchaseContract
{
    public void SetPurchaseContractItemCommittedQuantity(Guid childId, decimal? committedQuantity)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetCommittedQuantity(committedQuantity);
    }

    public void SetPurchaseContractItemOrderingRules(Guid childId, decimal minimumOrderQuantity, int? leadTimeDays)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetOrderingRules(minimumOrderQuantity, leadTimeDays);
    }

    public void SetPurchaseContractItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }

    public void RecordPurchaseContractItemConsumption(Guid childId, decimal quantity)
    {
        var child = GetChild(childId, _items);
        child.RecordConsumption(quantity);
    }

    public void ResetPurchaseContractItemConsumption(Guid childId)
    {
        var child = GetChild(childId, _items);
        child.ResetConsumption();
    }
}
