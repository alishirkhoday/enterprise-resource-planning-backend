using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CloseOpportunity
{
    /// <summary>
    /// Closes a deal, won or lost - بستن فرصت فروش.
    /// <para>
    /// A win also posts against whatever sales targets cover the salesperson on that date, and
    /// against the campaign that brought the deal in. Both are the moment the money becomes real,
    /// and leaving them to a nightly job means a scoreboard that is always a day behind the room.
    /// </para>
    /// <para>
    /// A loss records its reason and nothing else. It is deliberately not silent: lost reasons are
    /// the only free lesson a sales system produces.
    /// </para>
    /// </summary>
    public class CloseOpportunityCommandHandler(
        IOpportunityRepository opportunities,
        ISalesTargetRepository salesTargets,
        ICampaignRepository campaigns)
        : IRequestHandler<CloseOpportunityCommand, Result<OpportunityDto, Error>>
    {
        public async Task<Result<OpportunityDto, Error>> Handle(CloseOpportunityCommand request, CancellationToken cancellationToken)
        {
            var opportunity = await opportunities.GetByIdAsync(request.OpportunityId, cancellationToken);
            if (opportunity is null)
                return Result.Failure<OpportunityDto, Error>(Errors.General.NotFound(request.OpportunityId.ToString(), nameof(Opportunity)));

            // The closing date is deliberately not checked against when the row was created.
            // Importing historical deals writes the record today and closes it last year, and a
            // guard on that would refuse every migration while catching nothing that matters:
            // the domain already refuses to close a deal twice.
            try
            {
                if (request.IsWon)
                    opportunity.Win(request.ClosedOn, request.SaleOrderId);
                else
                    opportunity.Lose(request.ClosedOn, request.LostReason!);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<OpportunityDto, Error>(Errors.Marketing.OpportunityCannotBeChanged(exception.Message));
            }

            if (request.IsWon)
            {
                await CreditTargetsAsync(opportunity, request.ClosedOn, cancellationToken);
                await CreditCampaignAsync(opportunity, cancellationToken);
            }

            await opportunities.UpdateAsync(opportunity, cancellationToken);

            return Result.Success<OpportunityDto, Error>(opportunity.ToDto());
        }

        /// <summary>
        /// Every target covering the salesperson on the closing date is credited, not just one.
        /// A quarterly target inside an annual one both count the same sale, and picking only the
        /// narrowest would leave the annual figure permanently short.
        /// </summary>
        private async Task CreditTargetsAsync(Opportunity opportunity, DateOnly closedOn, CancellationToken cancellationToken)
        {
            if (opportunity.AssignedToHumanId is null)
                return;

            var targets = await salesTargets.GetForScopeAsync(
                SalesTargetScope.SalesRepresentative,
                opportunity.AssignedToHumanId.Value,
                closedOn,
                cancellationToken);

            foreach (var target in targets)
            {
                // A target set in another currency is left alone rather than credited at a rate
                // nobody supplied. Silently converting is how a scoreboard stops being checkable.
                if (target.TargetAmount.CurrencyCode != opportunity.EstimatedValue.CurrencyCode)
                    continue;

                target.RecordAchievement(opportunity.EstimatedValue, count: 1);
                await salesTargets.UpdateAsync(target, cancellationToken);
            }
        }

        private async Task CreditCampaignAsync(Opportunity opportunity, CancellationToken cancellationToken)
        {
            if (opportunity.CampaignId is null)
                return;

            var campaign = await campaigns.GetByIdAsync(opportunity.CampaignId.Value, cancellationToken);
            if (campaign is null || campaign.Budget.CurrencyCode != opportunity.EstimatedValue.CurrencyCode)
                return;

            campaign.RecordRevenue(opportunity.EstimatedValue);
            await campaigns.UpdateAsync(campaign, cancellationToken);
        }
    }
}
