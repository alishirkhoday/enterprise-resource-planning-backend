using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.IssueEmploymentContract
{
    /// <summary>
    /// Issues an employment contract.
    /// <para>
    /// Two contracts overlapping on one date is the failure this guards against. Payroll asks for
    /// "the contract effective on this day" and takes one answer; if two are effective, whichever
    /// the database returns first decides somebody's salary.
    /// </para>
    /// </summary>
    public class IssueEmploymentContractCommandHandler(
        IHumanRepository humans,
        IEmploymentContractRepository contracts)
        : IRequestHandler<IssueEmploymentContractCommand, Result<EmploymentContractDto, Error>>
    {
        public async Task<Result<EmploymentContractDto, Error>> Handle(IssueEmploymentContractCommand request, CancellationToken cancellationToken)
        {
            var human = await humans.GetWithContractsAsync(request.HumanId, cancellationToken);
            if (human is null)
                return Result.Failure<EmploymentContractDto, Error>(Errors.General.NotFound(request.HumanId.ToString(), nameof(Human)));

            if (human.Status == HumanStatus.Terminated)
                return Result.Failure<EmploymentContractDto, Error>(Errors.HumanResources.EmployeeIsTerminated(human.FullName));

            var existing = await contracts.GetEffectiveAsync(request.HumanId, request.StartDate, cancellationToken);
            if (existing is not null)
                return Result.Failure<EmploymentContractDto, Error>(
                    Errors.HumanResources.ContractOverlaps(human.FullName, request.StartDate));

            var position = await humans.GetPositionAsync(request.PositionId, cancellationToken);
            if (position is null)
                return Result.Failure<EmploymentContractDto, Error>(Errors.General.NotFound(request.PositionId.ToString(), nameof(Position)));

            var period = request.EndDate is null
                ? DateRange.Create(request.StartDate, null)
                : DateRange.Create(request.StartDate, request.EndDate.Value);

            var contract = new HumanContract(human, position, request.EmploymentType, period);
            contract.SetBaseSalary(Money.Create(request.BaseSalary, request.CurrencyCode));
            contract.SetContractNumber(request.ContractNumber);
            contract.SetWeeklyWorkingHours(request.WeeklyWorkingHours);
            contract.SetDescription(request.Description);

            if (request.ProbationEndDate is not null)
                contract.SetProbationEnd(request.ProbationEndDate.Value);

            if (request.CostCenterId is not null)
                contract.AssignCostCenter(request.CostCenterId.Value);

            if (request.WorkCalendarId is not null)
                contract.AssignWorkCalendar(request.WorkCalendarId.Value);

            // Somebody with a contract is employed, whatever their record said before. A rehire
            // arrives here as a new contract and has to clear the termination date with it.
            if (human.Status != HumanStatus.Active)
                human.Hire(request.StartDate);

            await contracts.AddAsync(contract, cancellationToken);

            return Result.Success<EmploymentContractDto, Error>(new EmploymentContractDto(
                contract.Id,
                human.Id,
                human.FullName,
                position.Id,
                contract.EmploymentType,
                contract.Period.StartDate,
                contract.Period.EndDate,
                contract.BaseSalary!.Amount,
                contract.BaseSalary.CurrencyCode,
                contract.ContractNumber,
                contract.ProbationEndDate));
        }
    }
}
