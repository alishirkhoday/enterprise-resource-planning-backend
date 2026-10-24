namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class SaleOrder
{
    public void SetSaleOrderItemDiscount(Guid childId, Percentage discountPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDiscount(discountPercentage);
    }

    public void SetSaleOrderItemTax(Guid childId, Guid taxId, Percentage taxPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTax(taxId, taxPercentage);
    }

    public void SetSaleOrderItemFulfillmentSource(Guid childId, Guid warehouseId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetFulfillmentSource(warehouseId);
    }

    public void SetSaleOrderItemPromisedDeliveryDate(Guid childId, DateOnly promisedDeliveryDate)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetPromisedDeliveryDate(promisedDeliveryDate);
    }

    public void SetSaleOrderItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }

    public void ChangeSaleOrderItemQuantity(Guid childId, decimal quantity)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.ChangeQuantity(quantity);
    }

    public void ReserveSaleOrderItem(Guid childId, decimal quantity)
    {
        if (Status is SaleOrderStatus.Draft or SaleOrderStatus.Cancelled or SaleOrderStatus.Closed)
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _items);
        child.Reserve(quantity);
    }

    public void ReleaseSaleOrderItemReservation(Guid childId, decimal quantity)
    {
        if (Status is SaleOrderStatus.Draft or SaleOrderStatus.Cancelled or SaleOrderStatus.Closed)
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _items);
        child.ReleaseReservation(quantity);
    }

    public void DeliverSaleOrderItem(Guid childId, decimal quantity)
    {
        if (Status is SaleOrderStatus.Draft or SaleOrderStatus.Cancelled or SaleOrderStatus.Closed)
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _items);
        child.Deliver(quantity);
    }

    public void ReverseSaleOrderItemDelivery(Guid childId, decimal quantity)
    {
        if (Status is SaleOrderStatus.Draft or SaleOrderStatus.Cancelled or SaleOrderStatus.Closed)
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _items);
        child.ReverseDelivery(quantity);
    }

    public void InvoiceSaleOrderItem(Guid childId, decimal quantity)
    {
        if (Status is SaleOrderStatus.Draft or SaleOrderStatus.Cancelled or SaleOrderStatus.Closed)
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _items);
        child.Invoice(quantity);
    }
}
