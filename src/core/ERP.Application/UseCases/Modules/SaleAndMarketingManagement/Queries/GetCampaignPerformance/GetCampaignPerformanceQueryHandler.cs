using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetCampaignPerformance
{
    /// <summary>
    /// Whether a campaign was worth running - عملکرد کمپین.
    /// <para>
    /// Cost per lead is reported alongside cost per conversion because the two disagree often,
    /// and the disagreement is the finding: a campaign with cheap leads and expensive conversions
    /// is buying the wrong people, and only the second number says so.
    /// </para>
    /// </summary>
    public class GetCampaignPerformanceQueryHandler(ICampaignRepository campaigns, ILeadRepository leads)
        : IRequestHandler<GetCampaignPerformanceQuery, Result<CampaignPerformanceDto, Error>>
    {
        public async Task<Result<CampaignPerformanceDto, Error>> Handle(GetCampaignPerformanceQuery request, CancellationToken cancellationToken)
        {
            var campaign = await campaigns.GetByIdAsync(request.CampaignId, cancellationToken);
            if (campaign is null)
                return Result.Failure<CampaignPerformanceDto, Error>(Errors.General.NotFound(request.CampaignId.ToString(), nameof(Campaign)));

            var (total, converted) = await leads.GetCampaignConversionAsync(campaign.Id, cancellationToken);

            var cost = campaign.ActualCost.Amount;
            var revenue = campaign.AttributedRevenue.Amount;

            // Every ratio is left null when its denominator is zero rather than reported as zero.
            // A campaign that has spent nothing has no cost per lead, and printing 0 would read
            // as the best campaign anybody ever ran.
            return Result.Success<CampaignPerformanceDto, Error>(new CampaignPerformanceDto(
                campaign.Id,
                campaign.Code,
                campaign.Name,
                campaign.Status,
                campaign.Budget.CurrencyCode,
                campaign.Budget.Amount,
                cost,
                revenue,
                revenue - cost,
                cost == 0m ? null : (revenue - cost) / cost * 100m,
                total,
                converted,
                total == 0 ? 0m : (decimal)converted / total * 100m,
                total == 0 ? null : cost / total,
                converted == 0 ? null : cost / converted,
                campaign.TargetLeadCount,
                cost > campaign.Budget.Amount));
        }
    }
}
