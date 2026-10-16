using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping
{
    public static class OpportunityMapper
    {
        public static OpportunityDto ToDto(this Opportunity opportunity)
        {
            ArgumentNullException.ThrowIfNull(opportunity, nameof(opportunity));

            return new OpportunityDto(
                opportunity.Id,
                opportunity.Title,
                opportunity.Stage,
                opportunity.CustomerId,
                opportunity.Customer?.DisplayName,
                opportunity.SourceLeadId,
                opportunity.EstimatedValue.CurrencyCode,
                opportunity.EstimatedValue.Amount,
                opportunity.Probability.Value,
                opportunity.WeightedValue.Amount,
                opportunity.ExpectedCloseDate,
                opportunity.ActualCloseDate,
                opportunity.CampaignId,
                opportunity.AssignedToHumanId,
                opportunity.QuotationId,
                opportunity.SaleOrderId,
                opportunity.CompetitorName,
                opportunity.LostReason,
                opportunity.IsOpen);
        }
    }
}
