using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RunPayroll
{
    public class RunPayrollCommandValidator : AbstractValidator<RunPayrollCommand>
    {
        public RunPayrollCommandValidator()
        {
            RuleFor(p => p.HumanId).NotEmpty().WithMessage("Whose payroll?");
            RuleFor(p => p.StartDate).NotEqual(default(DateOnly)).WithMessage("The period needs a start.");

            RuleFor(p => p.EndDate)
                .GreaterThan(p => p.StartDate)
                .WithMessage("A payroll period cannot end before it begins.");

            RuleFor(p => p.WorkedDays).GreaterThanOrEqualTo(0m);
            RuleFor(p => p.AbsentDays).GreaterThanOrEqualTo(0m);
            RuleFor(p => p.PaidLeaveDays).GreaterThanOrEqualTo(0m);
            RuleFor(p => p.UnpaidLeaveDays).GreaterThanOrEqualTo(0m);
            RuleFor(p => p.OvertimeHours).GreaterThanOrEqualTo(0m);

            // The days have to add up to a period that exists. More days accounted for than the
            // period holds means somebody has been paid for time that never happened.
            RuleFor(p => p)
                .Must(p => p.WorkedDays + p.AbsentDays + p.PaidLeaveDays + p.UnpaidLeaveDays
                           <= p.EndDate.DayNumber - p.StartDate.DayNumber + 1)
                .WithMessage("The days accounted for are more than the period contains.")
                .When(p => p.EndDate > p.StartDate);

            RuleForEach(p => p.AdditionalLines).SetValidator(new PayrollLineValidator());
        }
    }

    public class PayrollLineValidator : AbstractValidator<PayrollLineDto>
    {
        public PayrollLineValidator()
        {
            RuleFor(l => l.Code).NotEmpty().WithMessage("Every line needs a code.");
            RuleFor(l => l.Title).NotEmpty().WithMessage("Every line needs a title.");
            RuleFor(l => l.Nature).IsInEnum();
            RuleFor(l => l.Amount).GreaterThan(0m).WithMessage("A line for nothing is not a line.");
        }
    }
}
