namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class PurchaseOrder
{
    public void SetPurchaseOrderItemDiscount(Guid childId, Percentage discountPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDiscount(discountPercentage);
    }

    public void SetPurchaseOrderItemTax(Guid childId, Guid taxId, Percentage taxPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTax(taxId, taxPercentage);
    }

    public void SetPurchaseOrderItemExpectedDeliveryDate(Guid childId, DateOnly expectedDeliveryDate)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetExpectedDeliveryDate(expectedDeliveryDate);
    }

    public void SetPurchaseOrderItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }

    public void ChangePurchaseOrderItemQuantity(Guid childId, decimal quantity)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.ChangeQuantity(quantity);
    }

    public void ReceivePurchaseOrderItem(Guid childId, decimal quantity)
    {
        if (!CanReceive)
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _items);
        child.Receive(quantity);
        RefreshReceiptStatus();
    }

    public void ReversePurchaseOrderItemReceipt(Guid childId, decimal quantity)
    {
        if (Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received))
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _items);
        child.ReverseReceipt(quantity);
        RefreshReceiptStatus();
    }

    public void InvoicePurchaseOrderItem(Guid childId, decimal quantity)
    {
        if (Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received))
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _items);
        child.Invoice(quantity);
    }
}
