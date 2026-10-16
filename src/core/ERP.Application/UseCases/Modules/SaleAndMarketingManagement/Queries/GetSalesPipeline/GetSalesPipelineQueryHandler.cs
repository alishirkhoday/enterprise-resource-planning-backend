using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetSalesPipeline
{
    /// <summary>
    /// The pipeline - قیف فروش.
    /// <para>
    /// The weighted value is the number a forecast is built from, and it is reported next to the
    /// raw estimate on purpose: the gap between them is how much of the pipeline is hope. A board
    /// shown only the estimate will plan around money nobody has won yet.
    /// </para>
    /// </summary>
    public class GetSalesPipelineQueryHandler(IOpportunityRepository opportunities, TimeProvider timeProvider)
        : IRequestHandler<GetSalesPipelineQuery, Result<SalesPipelineDto, Error>>
    {
        /// <summary>
        /// Every stage a live deal can sit in, so the funnel reads the same shape every time and
        /// an empty stage says "nothing here" rather than "not asked".
        /// </summary>
        private static readonly OpportunityStage[] OpenStages =
        [
            OpportunityStage.Prospecting,
            OpportunityStage.Qualification,
            OpportunityStage.NeedsAnalysis,
            OpportunityStage.Proposal,
            OpportunityStage.Negotiation
        ];

        public async Task<Result<SalesPipelineDto, Error>> Handle(GetSalesPipelineQuery request, CancellationToken cancellationToken)
        {
            var asOf = request.AsOf ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            var open = await opportunities.GetOpenAsync(request.AssignedToHumanId, cancellationToken);

            var scoped = request.CurrencyCode is null
                ? open
                : [.. open.Where(o => o.EstimatedValue.CurrencyCode == request.CurrencyCode.Trim().ToUpperInvariant())];

            // Deals in different currencies cannot be added together without a rate nobody
            // supplied, so the totals below are only honest once one currency is picked. With
            // none given, the commonest one in the pipeline is reported and the rest left out.
            var currencyCode = request.CurrencyCode?.Trim().ToUpperInvariant()
                ?? scoped
                    .GroupBy(o => o.EstimatedValue.CurrencyCode)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault()
                ?? "IRR";

            var counted = scoped.Where(o => o.EstimatedValue.CurrencyCode == currencyCode).ToArray();

            var byStage = OpenStages
                .Select(stage =>
                {
                    var deals = counted.Where(o => o.Stage == stage).ToArray();

                    return new PipelineStageDto(
                        stage,
                        deals.Length,
                        deals.Sum(o => o.EstimatedValue.Amount),
                        deals.Sum(o => o.WeightedValue.Amount));
                })
                .ToArray();

            return Result.Success<SalesPipelineDto, Error>(new SalesPipelineDto(
                asOf,
                currencyCode,
                counted.Length,
                counted.Sum(o => o.EstimatedValue.Amount),
                counted.Sum(o => o.WeightedValue.Amount),
                counted.Count(o => o.IsOverdueOn(asOf)),
                byStage));
        }
    }
}
