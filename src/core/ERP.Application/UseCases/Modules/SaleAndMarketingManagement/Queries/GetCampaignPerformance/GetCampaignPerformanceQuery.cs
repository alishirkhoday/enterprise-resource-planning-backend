using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetCampaignPerformance
{
    /// <summary>What a campaign cost, what it brought in, and how many leads it converted.</summary>
    public sealed record GetCampaignPerformanceQuery : IQuery<Result<CampaignPerformanceDto, Error>>
    {
        public Guid CampaignId { get; init; }
    }
}
