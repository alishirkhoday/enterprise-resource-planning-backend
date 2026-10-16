using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.LaunchCampaign;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.RecordCampaignCost
{
    /// <summary>
    /// Books spend against a campaign - ثبت هزینه کمپین.
    /// <para>
    /// Costs are added as they are incurred rather than reconciled at the end, because the number
    /// worth having is the one available while the campaign can still be stopped. Overspending is
    /// recorded rather than refused: the money is already gone, and hiding it helps nobody.
    /// </para>
    /// </summary>
    public class RecordCampaignCostCommandHandler(ICampaignRepository campaigns)
        : IRequestHandler<RecordCampaignCostCommand, Result<CampaignDto, Error>>
    {
        public async Task<Result<CampaignDto, Error>> Handle(RecordCampaignCostCommand request, CancellationToken cancellationToken)
        {
            var campaign = await campaigns.GetByIdAsync(request.CampaignId, cancellationToken);
            if (campaign is null)
                return Result.Failure<CampaignDto, Error>(Errors.General.NotFound(request.CampaignId.ToString(), nameof(Campaign)));

            if (campaign.Status == CampaignStatus.Cancelled)
                return Result.Failure<CampaignDto, Error>(Errors.Marketing.CampaignCannotBeChanged(
                    "A cancelled campaign takes no further spend."));

            try
            {
                campaign.RecordCost(Money.Create(request.Amount, campaign.Budget.CurrencyCode));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return Result.Failure<CampaignDto, Error>(Errors.Marketing.CampaignCannotBeChanged(exception.Message));
            }

            await campaigns.UpdateAsync(campaign, cancellationToken);

            return Result.Success<CampaignDto, Error>(LaunchCampaignCommandHandler.ToDto(campaign));
        }
    }
}
