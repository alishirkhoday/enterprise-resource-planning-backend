namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class PurchaseInvoice
{
    public void LinkPurchaseInvoiceItemToOrderItem(Guid childId, Guid purchaseOrderItemId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.LinkToOrderItem(purchaseOrderItemId);
    }

    public void SetPurchaseInvoiceItemDiscount(Guid childId, Percentage discountPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDiscount(discountPercentage);
    }

    public void SetPurchaseInvoiceItemTax(Guid childId, Guid taxId, Percentage taxPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTax(taxId, taxPercentage);
    }

    public void SetPurchaseInvoiceItemPostingTargets(Guid childId, Guid? accountId, Guid? costCenterId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetPostingTargets(accountId, costCenterId);
    }

    public void SetPurchaseInvoiceItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }
}
