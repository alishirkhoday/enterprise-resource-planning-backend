using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.SetSalesTarget
{
    /// <summary>
    /// Sets a quota - هدف فروش.
    /// <para>
    /// Overlapping targets are allowed on purpose: a quarterly target sits inside an annual one
    /// and both are real. What matters is that a won deal credits every target covering its
    /// closing date, which is what closing an opportunity does.
    /// </para>
    /// </summary>
    public class SetSalesTargetCommandHandler(ISalesTargetRepository salesTargets)
        : IRequestHandler<SetSalesTargetCommand, Result<SalesTargetDto, Error>>
    {
        public async Task<Result<SalesTargetDto, Error>> Handle(SetSalesTargetCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var target = new SalesTarget(
                    request.Title,
                    request.Scope,
                    request.ScopeReferenceId,
                    DateRange.Create(request.StartDate, request.EndDate),
                    Money.Create(request.TargetAmount, request.CurrencyCode.Trim().ToUpperInvariant()));

                if (request.TargetCount is not null)
                    target.SetCountTarget(request.TargetCount.Value);

                if (request.ItemGroupId is not null)
                    target.NarrowToItemGroup(request.ItemGroupId.Value);

                if (request.BranchId is not null)
                    target.AssignToBranch(request.BranchId.Value);

                target.SetDescription(request.Description);

                await salesTargets.CreateAsync(target, cancellationToken);

                return Result.Success<SalesTargetDto, Error>(ToDto(target));
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result.Failure<SalesTargetDto, Error>(Errors.Marketing.CampaignCannotBeChanged(exception.Message));
            }
        }

        internal static SalesTargetDto ToDto(SalesTarget target) => new(
            target.Id,
            target.Title,
            target.Scope,
            target.ScopeReferenceId,
            target.Period.StartDate,
            target.Period.EndDate,
            target.TargetAmount.CurrencyCode,
            target.TargetAmount.Amount,
            target.AchievedAmount.Amount,
            target.Shortfall.Amount,
            target.AchievementPercentage,
            target.TargetCount,
            target.AchievedCount,
            target.IsMet,
            target.ItemGroupId);
    }
}
