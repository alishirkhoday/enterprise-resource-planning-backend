using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;
using ERP.Domain.Entities.Modules.ReportingManagement;
using ERP.Domain.Repositories.Modules.ReportingManagement;

namespace ERP.Application.UseCases.Modules.ReportingManagement.Commands.ScheduleReport
{
    /// <summary>
    /// Arranges for a report to be sent on a schedule - زمان‌بندی گزارش.
    /// <para>
    /// The cron expression is checked for shape here rather than being handed to the dispatcher
    /// to choke on later. A schedule that silently never fires is worse than one that is refused:
    /// nobody notices a report they were never getting.
    /// </para>
    /// </summary>
    public class ScheduleReportCommandHandler(IReportTemplateRepository templates)
        : IRequestHandler<ScheduleReportCommand, Result<ScheduledReportDto, Error>>
    {
        public async Task<Result<ScheduledReportDto, Error>> Handle(ScheduleReportCommand request, CancellationToken cancellationToken)
        {
            var template = await templates.GetByIdAsync(request.ReportTemplateId, cancellationToken);
            if (template is null)
                return Result.Failure<ScheduledReportDto, Error>(
                    Errors.General.NotFound(request.ReportTemplateId.ToString(), nameof(ReportTemplate)));

            var cron = request.CronExpression.Trim();

            if (!IsFiveFieldCron(cron))
                return Result.Failure<ScheduledReportDto, Error>(Errors.Reporting.CronExpressionIsInvalid(cron));

            var schedule = new ScheduledReport(template, cron, request.Recipients.Trim());

            if (!request.IsActive)
                schedule.Deactivate();

            await templates.AddScheduleAsync(schedule, cancellationToken);

            return Result.Success<ScheduledReportDto, Error>(new ScheduledReportDto(
                schedule.Id,
                template.Id,
                template.Name,
                schedule.CronExpression,
                schedule.Recipients,
                schedule.IsActive));
        }

        /// <summary>
        /// A shape check, not a full parse: five whitespace separated fields made only of the
        /// characters cron allows. Validating the ranges properly is the dispatcher's job, since
        /// it is the one that has to agree with its own parser.
        /// </summary>
        private static bool IsFiveFieldCron(string expression)
        {
            var fields = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return fields.Length == 5
                && fields.All(field => field.Length > 0 && field.All(c => char.IsAsciiDigit(c) || c is '*' or ',' or '-' or '/'));
        }
    }
}
