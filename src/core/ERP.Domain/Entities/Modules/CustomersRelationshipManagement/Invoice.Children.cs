using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

public sealed partial class Invoice
{
    public void SetInvoiceItemDiscount(Guid childId, Money discount)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDiscount(discount);
    }

    public void SetInvoiceItemDiscountPercentage(Guid childId, Percentage discountPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDiscountPercentage(discountPercentage);
    }

    public void SetInvoiceItemTax(Guid childId, Guid taxId, Percentage taxPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTax(taxId, taxPercentage);
    }

    public void LinkInvoiceItemToSaleOrderItem(Guid childId, Guid saleOrderItemId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.LinkToSaleOrderItem(saleOrderItemId);
    }

    public void SetInvoiceItemBatch(Guid childId, Guid itemBatchId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetBatch(itemBatchId);
    }

    public void SetInvoiceItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }
}
