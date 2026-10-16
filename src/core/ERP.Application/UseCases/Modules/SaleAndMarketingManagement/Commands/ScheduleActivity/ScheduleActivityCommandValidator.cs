using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ScheduleActivity
{
    public class ScheduleActivityCommandValidator : AbstractValidator<ScheduleActivityCommand>
    {
        public ScheduleActivityCommandValidator()
        {
            Include(new ScheduleActivityDtoValidator());
        }
    }

    /// <summary>
    /// Shared with the follow up on a completion, which is the same request arriving nested.
    /// </summary>
    public class ScheduleActivityDtoValidator : AbstractValidator<ScheduleActivityDto>
    {
        public ScheduleActivityDtoValidator()
        {
            RuleFor(r => r.Subject)
                .NotEmpty().WithMessage("What is the activity?")
                .MaximumLength(200);

            RuleFor(r => r.Type).IsInEnum();
            RuleFor(r => r.Priority).IsInEnum();

            // An activity pointing at nothing drops out of every list that matters.
            RuleFor(r => r.ReferenceType)
                .NotEmpty().WithMessage("What is it about?")
                .MaximumLength(100);

            RuleFor(r => r.ReferenceId).NotEmpty().WithMessage("Which one?");

            RuleFor(r => r.DueAt).NotEqual(default(DateTimeOffset)).WithMessage("When is it due?");

            RuleFor(r => r.AssignedToHumanId).NotEmpty().WithMessage("Who is doing it?");

            RuleFor(r => r.RemindAt)
                .LessThanOrEqualTo(r => r.DueAt)
                .WithMessage("A reminder after the activity is due reminds nobody of anything.")
                .When(r => r.RemindAt is not null);

            RuleFor(r => r.Description).MaximumLength(2000);
        }
    }
}
