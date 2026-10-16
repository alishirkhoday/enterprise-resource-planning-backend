using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Mapping
{
    public static class InvoiceMapper
    {
        public static InvoiceDto ToDto(this Invoice invoice)
        {
            ArgumentNullException.ThrowIfNull(invoice, nameof(invoice));

            return new InvoiceDto(
                invoice.Id,
                invoice.Number,
                invoice.Type,
                invoice.Status,
                invoice.CustomerId,
                invoice.Customer.DisplayName,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.CurrencyCode,
                invoice.TotalAmount.Amount,
                invoice.TotalDiscounts.Amount,
                invoice.TotalTax.Amount,
                invoice.AdditionalCharges.Amount,
                invoice.AmountPayable.Amount,
                invoice.OriginalInvoiceId,
                [.. invoice.Items
                    .OrderBy(i => i.LineNumber)
                    .Select(i => new InvoiceLineViewDto(
                        i.Id,
                        i.LineNumber,
                        i.ItemId,
                        i.Item.Name,
                        i.Quantity,
                        i.ItemUnitPrice.Amount,
                        i.Discount.Amount,
                        i.TaxAmount.Amount,
                        i.AmountPayableWithTax.Amount))]);
        }
    }
}
