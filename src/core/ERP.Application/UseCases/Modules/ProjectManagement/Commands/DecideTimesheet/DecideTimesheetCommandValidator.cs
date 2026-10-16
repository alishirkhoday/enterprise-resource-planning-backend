namespace ERP.Application.UseCases.Modules.ProjectManagement.Commands.DecideTimesheet
{
    public class DecideTimesheetCommandValidator : AbstractValidator<DecideTimesheetCommand>
    {
        public DecideTimesheetCommandValidator()
        {
            RuleFor(t => t.TimesheetId).NotEmpty().WithMessage("Which timesheet?");

            RuleFor(t => t.ApprovedByHumanId)
                .NotEmpty()
                .WithMessage("An approval has to say who gave it.")
                .When(t => t.IsApproved);

            RuleFor(t => t.Reason)
                .NotEmpty()
                .WithMessage("A rejected timesheet needs a reason, or nobody knows what to correct.")
                .When(t => !t.IsApproved);
        }
    }
}
