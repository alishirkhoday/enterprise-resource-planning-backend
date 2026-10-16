using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.EarnCommission
{
    /// <summary>
    /// Records commission earned on a sale - پورسانت فروش.
    /// <para>
    /// It is accrued, not owed. Commission on an invoice the customer has not paid is a liability
    /// against money that may never arrive, so it sits accrued until somebody approves it, and
    /// only then reaches payroll.
    /// </para>
    /// </summary>
    public class EarnCommissionCommandHandler(ICommissionRepository commissions)
        : IRequestHandler<EarnCommissionCommand, Result<CommissionDto, Error>>
    {
        public async Task<Result<CommissionDto, Error>> Handle(EarnCommissionCommand request, CancellationToken cancellationToken)
        {
            var referenceType = request.ReferenceType.Trim();
            var referenceId = request.ReferenceId.Trim();

            // Paying twice on one invoice is the mistake nobody notices until the salesperson
            // does, and by then it has been in three payroll runs.
            var existing = await commissions.GetBySourceAsync(referenceType, referenceId, cancellationToken);
            if (existing is not null)
                return Result.Failure<CommissionDto, Error>(Errors.Marketing.CommissionAlreadyEarned(referenceType, referenceId));

            try
            {
                var commission = new Commission(
                    request.SalesRepHumanId,
                    referenceType,
                    referenceId,
                    request.EarnedOn,
                    Money.Create(request.BaseAmount, request.CurrencyCode.Trim().ToUpperInvariant()),
                    Percentage.Create(request.Rate));

                commission.SetDescription(request.Description);

                await commissions.CreateAsync(commission, cancellationToken);

                return Result.Success<CommissionDto, Error>(ToDto(commission));
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result.Failure<CommissionDto, Error>(Errors.Marketing.CommissionCannotBeChanged(exception.Message));
            }
        }

        internal static CommissionDto ToDto(Commission commission) => new(
            commission.Id,
            commission.SalesRepHumanId,
            commission.ReferenceType,
            commission.ReferenceId,
            commission.EarnedOn,
            commission.Amount.CurrencyCode,
            commission.BaseAmount.Amount,
            commission.Rate.Value,
            commission.Amount.Amount,
            commission.Status,
            commission.PaidOn,
            commission.PayrollId,
            commission.Description);
    }
}
