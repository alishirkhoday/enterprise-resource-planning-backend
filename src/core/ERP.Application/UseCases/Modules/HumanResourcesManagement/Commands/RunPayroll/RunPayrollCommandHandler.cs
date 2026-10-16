using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Mapping;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RunPayroll
{
    /// <summary>
    /// Builds a payroll for one person over one period, as a draft.
    /// <para>
    /// The base salary is read from the contract effective in that period, not from the request.
    /// A salary that can be typed into the run is a salary that can be typed wrongly, and payroll
    /// is the one place where a wrong number leaves the building before anybody notices.
    /// </para>
    /// </summary>
    public class RunPayrollCommandHandler(
        IHumanRepository humans,
        IEmploymentContractRepository contracts,
        IPayrollRepository payrolls)
        : IRequestHandler<RunPayrollCommand, Result<PayrollDto, Error>>
    {
        public async Task<Result<PayrollDto, Error>> Handle(RunPayrollCommand request, CancellationToken cancellationToken)
        {
            var human = await humans.GetByIdAsync(request.HumanId, cancellationToken);
            if (human is null)
                return Result.Failure<PayrollDto, Error>(Errors.General.NotFound(request.HumanId.ToString(), nameof(Human)));

            // Running the same period twice is the single most expensive mistake this module can
            // make, so it is refused rather than merged.
            if (await payrolls.ExistsForPeriodAsync(request.HumanId, request.StartDate, request.EndDate, cancellationToken))
                return Result.Failure<PayrollDto, Error>(
                    Errors.HumanResources.PayrollAlreadyRun(human.FullName, request.StartDate, request.EndDate));

            var contract = await contracts.GetEffectiveAsync(request.HumanId, request.EndDate, cancellationToken);
            if (contract is null)
                return Result.Failure<PayrollDto, Error>(Errors.HumanResources.NoEffectiveContract(human.FullName, request.EndDate));

            if (contract.BaseSalary is null)
                return Result.Failure<PayrollDto, Error>(Errors.HumanResources.ContractHasNoSalary(human.FullName));

            var payroll = new Payroll(human, request.StartDate, request.EndDate, contract.BaseSalary);

            payroll.ApplyStructure(request.SalaryStructureId ?? Guid.Empty, contract.Id);

            payroll.RecordAttendance(
                request.WorkedDays,
                request.AbsentDays,
                request.PaidLeaveDays,
                request.UnpaidLeaveDays,
                request.OvertimeHours);

            if (request.CostCenterId is not null)
                payroll.AssignCostCenter(request.CostCenterId.Value);
            else if (contract.CostCenterId is not null)
                payroll.AssignCostCenter(contract.CostCenterId.Value);

            if (request.BranchId is not null)
                payroll.AssignToBranch(request.BranchId.Value);
            else if (human.BranchId is not null)
                payroll.AssignToBranch(human.BranchId.Value);

            // The base salary is a line like any other, so gross pay is the sum of the lines and
            // never a figure computed two different ways in two different places.
            payroll.AddItem(new PayrollItem(
                payroll,
                "BASE",
                "Base salary",
                SalaryItemNature.Earning,
                contract.BaseSalary));

            foreach (var line in request.AdditionalLines)
            {
                payroll.AddItem(new PayrollItem(
                    payroll,
                    line.Code,
                    line.Title,
                    line.Nature,
                    Money.Create(line.Amount, payroll.CurrencyCode)));
            }

            await payrolls.CreateAsync(payroll, cancellationToken);

            return Result.Success<PayrollDto, Error>(payroll.ToDto());
        }
    }
}
