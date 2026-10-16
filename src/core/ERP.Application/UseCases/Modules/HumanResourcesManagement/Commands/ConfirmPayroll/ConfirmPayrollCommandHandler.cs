using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Mapping;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.ConfirmPayroll
{
    /// <summary>
    /// Makes a payroll final and records that fact, which is what a payment run and the journal
    /// entry behind it both wait for.
    /// </summary>
    public class ConfirmPayrollCommandHandler(IPayrollRepository payrolls)
        : IRequestHandler<ConfirmPayrollCommand, Result<PayrollDto, Error>>
    {
        public async Task<Result<PayrollDto, Error>> Handle(ConfirmPayrollCommand request, CancellationToken cancellationToken)
        {
            var payroll = await payrolls.GetWithItemsAsync(request.PayrollId, cancellationToken);
            if (payroll is null)
                return Result.Failure<PayrollDto, Error>(Errors.General.NotFound(request.PayrollId.ToString(), nameof(Payroll)));

            try
            {
                // The payroll refuses a negative net itself. Somebody whose deductions exceed their
                // pay is a data problem, and confirming it would send a demand for money instead
                // of a payslip.
                payroll.Confirm();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<PayrollDto, Error>(Errors.HumanResources.PayrollCannotBeConfirmed(exception.Message));
            }

            await payrolls.UpdateAsync(payroll, cancellationToken);

            return Result.Success<PayrollDto, Error>(payroll.ToDto());
        }
    }
}
