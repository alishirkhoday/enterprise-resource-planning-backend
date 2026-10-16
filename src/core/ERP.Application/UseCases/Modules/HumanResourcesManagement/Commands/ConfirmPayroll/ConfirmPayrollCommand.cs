using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.ConfirmPayroll
{
    /// <summary>Makes a payroll final. After this it can only be reopened, never quietly adjusted.</summary>
    public sealed record ConfirmPayrollCommand : ICommand<Result<PayrollDto, Error>>
    {
        public Guid PayrollId { get; init; }
    }
}
