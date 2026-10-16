using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.EarnCommission;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ApproveCommission
{
    /// <summary>
    /// Confirms commission is really owed, or takes it back - تأیید یا برگشت پورسانت.
    /// <para>
    /// Approving is what moves the money from a provision to a debt, so it is a separate act from
    /// earning it and normally waits until the customer has paid. Reversing is how a commission on
    /// a sale that was later credited gets undone before it reaches payroll.
    /// </para>
    /// </summary>
    public class ApproveCommissionCommandHandler(ICommissionRepository commissions)
        : IRequestHandler<ApproveCommissionCommand, Result<CommissionDto, Error>>
    {
        public async Task<Result<CommissionDto, Error>> Handle(ApproveCommissionCommand request, CancellationToken cancellationToken)
        {
            var commission = await commissions.GetByIdAsync(request.CommissionId, cancellationToken);
            if (commission is null)
                return Result.Failure<CommissionDto, Error>(Errors.General.NotFound(request.CommissionId.ToString(), nameof(Commission)));

            try
            {
                if (request.IsApproved)
                    commission.Approve();
                else
                    commission.Reverse(request.ReversalReason!);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<CommissionDto, Error>(Errors.Marketing.CommissionCannotBeChanged(exception.Message));
            }

            await commissions.UpdateAsync(commission, cancellationToken);

            return Result.Success<CommissionDto, Error>(EarnCommissionCommandHandler.ToDto(commission));
        }
    }
}
