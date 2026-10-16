using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ScheduleActivity;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CompleteActivity
{
    public class CompleteActivityCommandValidator : AbstractValidator<CompleteActivityCommand>
    {
        public CompleteActivityCommandValidator()
        {
            RuleFor(r => r.ActivityId).NotEmpty().WithMessage("Which activity?");

            // A completion with no outcome tells the next person nothing, which is the same as
            // it never having been done.
            RuleFor(r => r.Outcome)
                .NotEmpty().WithMessage("What came of it?")
                .MaximumLength(2000);

            RuleFor(r => r.DurationMinutes)
                .GreaterThan(0).WithMessage("An activity that took no time did not happen.")
                .When(r => r.DurationMinutes is not null);

            RuleFor(r => r.FollowUp!)
                .SetValidator(new ScheduleActivityDtoValidator())
                .When(r => r.FollowUp is not null);
        }
    }
}
