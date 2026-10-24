namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class SupplierQuotation
{
    public void SetSupplierQuotationItemDiscount(Guid childId, Percentage discountPercentage)
    {
        EnsureOpen();
        var child = GetChild(childId, _items);
        child.SetDiscount(discountPercentage);
    }

    public void SetSupplierQuotationItemLeadTime(Guid childId, int leadTimeDays)
    {
        EnsureOpen();
        var child = GetChild(childId, _items);
        child.SetLeadTime(leadTimeDays);
    }

    public void SetSupplierQuotationItemSupplierItemCode(Guid childId, string? supplierItemCode)
    {
        EnsureOpen();
        var child = GetChild(childId, _items);
        child.SetSupplierItemCode(supplierItemCode);
    }

    public void SetSupplierQuotationItemDescription(Guid childId, string? description)
    {
        EnsureOpen();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }
}
