using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.AdvanceOpportunity
{
    /// <summary>
    /// Moves a deal along the pipeline - جابه‌جایی مرحله فرصت.
    /// <para>
    /// Moving the stage resets the probability to the one that stage implies, unless the request
    /// overrides it. That default is what makes a forecast comparable between salespeople: left
    /// to their own numbers, every deal sits at ninety percent until the day it is lost.
    /// </para>
    /// <para>
    /// Closing is deliberately not possible here. Won and lost need a date and, on a loss, a
    /// reason, so they go through their own command rather than slipping in as a stage change.
    /// </para>
    /// </summary>
    public class AdvanceOpportunityCommandHandler(IOpportunityRepository opportunities)
        : IRequestHandler<AdvanceOpportunityCommand, Result<OpportunityDto, Error>>
    {
        public async Task<Result<OpportunityDto, Error>> Handle(AdvanceOpportunityCommand request, CancellationToken cancellationToken)
        {
            var opportunity = await opportunities.GetByIdAsync(request.OpportunityId, cancellationToken);
            if (opportunity is null)
                return Result.Failure<OpportunityDto, Error>(Errors.General.NotFound(request.OpportunityId.ToString(), nameof(Opportunity)));

            if (request.Stage is OpportunityStage.ClosedWon or OpportunityStage.ClosedLost)
                return Result.Failure<OpportunityDto, Error>(Errors.Marketing.OpportunityCannotBeChanged(
                    "Winning or losing a deal is recorded by closing it, not by moving its stage."));

            try
            {
                if (request.EstimatedValue is not null || request.ExpectedCloseDate is not null)
                {
                    opportunity.Update(
                        opportunity.Title,
                        request.EstimatedValue is not null
                            ? Money.Create(request.EstimatedValue.Value, opportunity.EstimatedValue.CurrencyCode)
                            : opportunity.EstimatedValue,
                        request.ExpectedCloseDate ?? opportunity.ExpectedCloseDate);
                }

                opportunity.MoveToStage(request.Stage);

                // After the move, so an explicit probability wins over the stage's default rather
                // than being overwritten by it.
                if (request.Probability is not null)
                    opportunity.SetProbability(Percentage.Create(request.Probability.Value));

                if (request.CompetitorName is not null)
                    opportunity.SetCompetitor(request.CompetitorName);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
            {
                return Result.Failure<OpportunityDto, Error>(Errors.Marketing.OpportunityCannotBeChanged(exception.Message));
            }

            await opportunities.UpdateAsync(opportunity, cancellationToken);

            return Result.Success<OpportunityDto, Error>(opportunity.ToDto());
        }
    }
}
