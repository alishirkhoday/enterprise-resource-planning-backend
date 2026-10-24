using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

public sealed partial class Quotation
{
    public void SetQuotationItemDiscount(Guid childId, Percentage discountPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDiscount(discountPercentage);
    }

    public void SetQuotationItemTax(Guid childId, Guid taxId, Percentage taxPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTax(taxId, taxPercentage);
    }

    public void SetQuotationItemDeliveryLeadTime(Guid childId, int days)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDeliveryLeadTime(days);
    }

    public void SetQuotationItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }
}
