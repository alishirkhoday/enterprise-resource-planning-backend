namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.ConfirmPayroll
{
    public class ConfirmPayrollCommandValidator : AbstractValidator<ConfirmPayrollCommand>
    {
        public ConfirmPayrollCommandValidator()
        {
            RuleFor(p => p.PayrollId).NotEmpty().WithMessage("Which payroll?");
        }
    }
}
