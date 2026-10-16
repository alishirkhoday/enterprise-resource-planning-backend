using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Commands.SubmitTimesheet
{
    public class SubmitTimesheetCommandValidator : AbstractValidator<SubmitTimesheetCommand>
    {
        /// <summary>Nobody works more than this in a day, and a sheet that says so is a typing error.</summary>
        private const decimal MaximumHoursPerDay = 24m;

        public SubmitTimesheetCommandValidator()
        {
            RuleFor(t => t.HumanId).NotEmpty().WithMessage("Whose timesheet?");
            RuleFor(t => t.PeriodStart).NotEqual(default(DateOnly)).WithMessage("The period needs a start.");

            RuleFor(t => t.PeriodEnd)
                .GreaterThanOrEqualTo(t => t.PeriodStart)
                .WithMessage("A period cannot end before it begins.");

            RuleFor(t => t.Entries).NotEmpty().WithMessage("An empty timesheet cannot be submitted.");

            RuleForEach(t => t.Entries).SetValidator(new TimesheetEntryValidator());

            RuleFor(t => t.Entries)
                .Must(entries => entries.GroupBy(e => e.WorkDate).All(g => g.Sum(e => e.Hours) <= MaximumHoursPerDay))
                .WithMessage($"A day cannot hold more than {MaximumHoursPerDay} hours.")
                .When(t => t.Entries.Count > 0);

            // Time booked outside the period is either the wrong sheet or the wrong date, and
            // guessing which would put the hours somewhere nobody looks.
            RuleFor(t => t.Entries)
                .Must((command, entries) => entries.All(e => e.WorkDate >= command.PeriodStart && e.WorkDate <= command.PeriodEnd))
                .WithMessage("Every entry has to fall inside the period.")
                .When(t => t.Entries.Count > 0 && t.PeriodEnd >= t.PeriodStart);
        }
    }

    public class TimesheetEntryValidator : AbstractValidator<TimesheetEntryDto>
    {
        public TimesheetEntryValidator()
        {
            RuleFor(e => e.WorkDate).NotEqual(default(DateOnly)).WithMessage("Every entry needs a date.");
            RuleFor(e => e.Hours).GreaterThan(0m).WithMessage("An entry of no hours is not an entry.");

            // A line is against a task or against an activity, never neither: without one of them
            // the hours belong to nothing and can never be costed or billed.
            RuleFor(e => e)
                .Must(e => e.ProjectTaskId is not null || e.ActivityType is not null)
                .WithMessage("Every entry names a project task or an activity type.");

            RuleFor(e => e.BillingRate)
                .GreaterThanOrEqualTo(0m)
                .WithMessage("A rate cannot be negative.")
                .When(e => e.BillingRate is not null);
        }
    }
}
