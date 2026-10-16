namespace ERP.Application.UseCases.Modules.ReportingManagement.Commands.ScheduleReport
{
    public class ScheduleReportCommandValidator : AbstractValidator<ScheduleReportCommand>
    {
        public ScheduleReportCommandValidator()
        {
            RuleFor(r => r.ReportTemplateId).NotEmpty().WithMessage("Which report?");

            RuleFor(r => r.CronExpression)
                .NotEmpty().WithMessage("When should it be sent?")
                .MaximumLength(100);

            // A schedule with no recipients fires forever and reaches nobody.
            RuleFor(r => r.Recipients)
                .NotEmpty().WithMessage("Who receives it?")
                .MaximumLength(2000);
        }
    }
}
