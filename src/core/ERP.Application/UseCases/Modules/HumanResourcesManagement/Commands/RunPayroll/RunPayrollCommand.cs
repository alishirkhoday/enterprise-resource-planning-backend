using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RunPayroll
{
    public record RunPayrollCommand : RunPayrollDto, ICommand<Result<PayrollDto, Error>>;
}
