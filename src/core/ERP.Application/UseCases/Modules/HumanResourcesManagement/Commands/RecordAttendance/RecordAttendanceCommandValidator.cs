namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RecordAttendance
{
    public class RecordAttendanceCommandValidator : AbstractValidator<RecordAttendanceCommand>
    {
        public RecordAttendanceCommandValidator()
        {
            RuleFor(a => a.HumanId).NotEmpty().WithMessage("Whose attendance?");
            RuleFor(a => a.Date).NotEqual(default(DateOnly)).WithMessage("Which day?");

            // A record with neither time on it says nothing happened, which is not something to
            // record - it is the absence of a record.
            RuleFor(a => a)
                .Must(a => a.EntryTime is not null || a.ExitTime is not null)
                .WithMessage("An attendance record needs an entry or an exit time.");

            RuleFor(a => a.ExitTime)
                .GreaterThan(a => a.EntryTime!.Value)
                .WithMessage("The exit cannot come before the entry.")
                .When(a => a.EntryTime is not null && a.ExitTime is not null);
        }
    }
}
