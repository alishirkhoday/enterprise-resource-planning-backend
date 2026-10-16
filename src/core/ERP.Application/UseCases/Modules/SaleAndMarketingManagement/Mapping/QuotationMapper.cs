using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping
{
    public static class QuotationMapper
    {
        public static QuotationDto ToDto(this Quotation quotation)
        {
            ArgumentNullException.ThrowIfNull(quotation, nameof(quotation));

            return new QuotationDto(
                quotation.Id,
                quotation.Number,
                quotation.Version,
                quotation.Status,
                quotation.CustomerId,
                quotation.Customer.DisplayName,
                quotation.IssueDate,
                quotation.ExpiryDate,
                quotation.CurrencyCode,
                quotation.SubTotal().Amount,
                quotation.HeaderDiscountAmount().Amount,
                quotation.TotalTax().Amount,
                quotation.AdditionalCharges.Amount,
                quotation.GrandTotal().Amount,
                quotation.OpportunityId,
                quotation.SaleOrderId,
                quotation.Terms,
                quotation.RejectionReason,
                [.. quotation.Items
                    .OrderBy(i => i.LineNumber)
                    .Select(i => new QuotationLineViewDto(
                        i.Id,
                        i.LineNumber,
                        i.ItemId,
                        i.Item.Name,
                        i.Quantity,
                        i.UnitPrice.Amount,
                        i.DiscountAmount.Amount,
                        i.TaxAmount.Amount,
                        i.LineTotalWithTax.Amount,
                        i.DeliveryLeadTimeDays))]);
        }
    }
}
