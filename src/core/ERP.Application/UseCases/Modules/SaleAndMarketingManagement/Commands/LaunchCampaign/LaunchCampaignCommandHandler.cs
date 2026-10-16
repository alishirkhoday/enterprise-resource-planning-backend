using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.LaunchCampaign
{
    /// <summary>
    /// Sets up a campaign and starts it - کمپین بازاریابی.
    /// <para>
    /// The budget is the reason this is an entity and not a spreadsheet. A campaign that records
    /// what it cost and what it brought in can be answered for; one that only counts leads always
    /// looks successful, because attracting the wrong people is cheap.
    /// </para>
    /// </summary>
    public class LaunchCampaignCommandHandler(ICampaignRepository campaigns)
        : IRequestHandler<LaunchCampaignCommand, Result<CampaignDto, Error>>
    {
        public async Task<Result<CampaignDto, Error>> Handle(LaunchCampaignCommand request, CancellationToken cancellationToken)
        {
            var code = request.Code.Trim();

            if (await campaigns.IsExistenceByCodeAsync(code, cancellationToken: cancellationToken))
                return Result.Failure<CampaignDto, Error>(Errors.Marketing.CampaignCodeIsUsed(code));

            try
            {
                var campaign = new Campaign(
                    code,
                    request.Name,
                    request.Channel,
                    DateRange.Create(request.StartDate, request.EndDate),
                    Money.Create(request.Budget, request.CurrencyCode.Trim().ToUpperInvariant()));

                campaign.Update(request.Name, request.Channel, request.Description);
                campaign.SetTargets(request.TargetLeadCount, request.CustomerGroupId);

                if (request.OwnerHumanId is not null)
                    campaign.AssignOwner(request.OwnerHumanId.Value);

                if (request.BranchId is not null)
                    campaign.AssignToBranch(request.BranchId.Value);

                if (request.StartImmediately)
                    campaign.Launch();

                await campaigns.CreateAsync(campaign, cancellationToken);

                return Result.Success<CampaignDto, Error>(ToDto(campaign));
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
            {
                return Result.Failure<CampaignDto, Error>(Errors.Marketing.CampaignCannotBeChanged(exception.Message));
            }
        }

        internal static CampaignDto ToDto(Campaign campaign) => new(
            campaign.Id,
            campaign.Code,
            campaign.Name,
            campaign.Channel,
            campaign.Status,
            campaign.StartDate,
            campaign.EndDate,
            campaign.Budget.CurrencyCode,
            campaign.Budget.Amount,
            campaign.ActualCost.Amount,
            campaign.AttributedRevenue.Amount,
            campaign.TargetLeadCount,
            campaign.OwnerHumanId);
    }
}
