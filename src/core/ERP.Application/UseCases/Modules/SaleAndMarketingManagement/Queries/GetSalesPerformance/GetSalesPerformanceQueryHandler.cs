using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.SetSalesTarget;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetSalesPerformance
{
    /// <summary>
    /// How a salesperson is doing - کارنامه فروش.
    /// <para>
    /// Target, won, pipeline and commission are reported together because each one alone misleads.
    /// Somebody at sixty percent of target with a full pipeline is in a different position from
    /// somebody at sixty percent with nothing left to close, and only seeing both says which.
    /// </para>
    /// </summary>
    public class GetSalesPerformanceQueryHandler(
        ISalesTargetRepository salesTargets,
        IOpportunityRepository opportunities,
        ICommissionRepository commissions,
        IActivityRepository activities,
        TimeProvider timeProvider)
        : IRequestHandler<GetSalesPerformanceQuery, Result<SalesPerformanceDto, Error>>
    {
        public async Task<Result<SalesPerformanceDto, Error>> Handle(GetSalesPerformanceQuery request, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow();
            var today = DateOnly.FromDateTime(now.UtcDateTime);

            var to = request.To ?? today;
            var from = request.From ?? new DateOnly(to.Year, 1, 1);

            var targets = await salesTargets.GetForScopeAsync(
                Domain.Entities.Modules.SaleAndMarketingManagement.SalesTargetScope.SalesRepresentative,
                request.SalesRepHumanId,
                to,
                cancellationToken);

            var open = await opportunities.GetOpenAsync(request.SalesRepHumanId, cancellationToken);
            var commission = await commissions.GetEarnedTotalAsync(request.SalesRepHumanId, from, to, cancellationToken);
            var openActivities = await activities.GetOpenForAsync(request.SalesRepHumanId, cancellationToken);

            // The currency comes from the target rather than the deals, because the target is what
            // the other figures are being compared against. With no target, the pipeline's own
            // currency is the only one available.
            var currencyCode = targets.Count > 0
                ? targets[0].TargetAmount.CurrencyCode
                : open.FirstOrDefault()?.EstimatedValue.CurrencyCode ?? "IRR";

            var comparable = targets.Where(t => t.TargetAmount.CurrencyCode == currencyCode).ToArray();
            var pipeline = open.Where(o => o.EstimatedValue.CurrencyCode == currencyCode).ToArray();

            var targetAmount = comparable.Sum(t => t.TargetAmount.Amount);
            var achievedAmount = comparable.Sum(t => t.AchievedAmount.Amount);

            return Result.Success<SalesPerformanceDto, Error>(new SalesPerformanceDto(
                request.SalesRepHumanId,
                from,
                to,
                currencyCode,
                targetAmount,
                achievedAmount,
                targetAmount == 0m ? 0m : achievedAmount / targetAmount * 100m,
                pipeline.Sum(o => o.EstimatedValue.Amount),
                pipeline.Sum(o => o.WeightedValue.Amount),
                commission,
                pipeline.Length,
                openActivities.Count,
                openActivities.Count(a => a.DueAt is not null && a.DueAt < now),
                [.. comparable.Select(SetSalesTargetCommandHandler.ToDto)]));
        }
    }
}
