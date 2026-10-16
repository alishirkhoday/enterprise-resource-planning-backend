using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Mapping
{
    public static class PurchaseOrderMapper
    {
        public static PurchaseOrderDto ToDto(this PurchaseOrder order)
        {
            ArgumentNullException.ThrowIfNull(order, nameof(order));

            return new PurchaseOrderDto(
                order.Id,
                order.Number,
                order.SupplierId,
                order.Supplier.Name,
                order.Status,
                order.CurrencyCode,
                order.ExpectedDeliveryDate,
                order.SubTotal().Amount,
                order.TotalTax().Amount,
                order.AdditionalCosts.Amount,
                order.GrandTotal().Amount,
                [.. order.Items
                    .OrderBy(i => i.LineNumber)
                    .Select(i => new PurchaseOrderLineViewDto(
                        i.Id,
                        i.LineNumber,
                        i.ItemId,
                        i.Item.Name,
                        i.Quantity,
                        i.ReceivedQuantity,
                        i.UnitPrice.Amount,
                        i.LineTotal.Amount))]);
        }
    }
}
