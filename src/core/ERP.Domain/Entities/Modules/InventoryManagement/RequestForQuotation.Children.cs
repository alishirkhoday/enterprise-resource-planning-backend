namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class RequestForQuotation
{
    public void SetRequestForQuotationItemRequiredByDate(Guid childId, DateOnly requiredByDate)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetRequiredByDate(requiredByDate);
    }

    public void SetRequestForQuotationItemSpecification(Guid childId, string? specification)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetSpecification(specification);
    }
}
