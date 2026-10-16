using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RequestLeave
{
    public class RequestLeaveCommandValidator : AbstractValidator<RequestLeaveCommand>
    {
        public RequestLeaveCommandValidator()
        {
            RuleFor(r => r.HumanId).NotEmpty().WithMessage("Whose leave?");
            RuleFor(r => r.Type).IsInEnum();
            RuleFor(r => r.StartDate).NotEqual(default(DateOnly)).WithMessage("From when?");

            RuleFor(r => r.EndDate)
                .GreaterThanOrEqualTo(r => r.StartDate)
                .WithMessage("Leave cannot end before it starts.");

            // Daily leave draws on an allowance, and which allowance is not something to guess.
            RuleFor(r => r.LeaveType)
                .NotNull()
                .WithMessage("Daily leave has to say which kind it is.")
                .When(r => r.Type == HumanRequestType.LeaveDaily);

            RuleFor(r => r.StartTime)
                .NotNull()
                .WithMessage("Hourly leave needs the hours.")
                .When(r => r.Type == HumanRequestType.LeaveHourly);

            RuleFor(r => r.EndTime)
                .NotNull()
                .WithMessage("Hourly leave needs the hours.")
                .When(r => r.Type == HumanRequestType.LeaveHourly);

            RuleFor(r => r.Reason)
                .NotEmpty()
                .WithMessage("Leave recorded as other has to say what it is.")
                .When(r => r.LeaveType == HumanRequestLeaveType.Other);
        }
    }
}
