using ERP.Application.UseCases.Modules.ReportingManagement.Commands.ScheduleReport;
using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ReportingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Reporting/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ReportingSwaggerGroup)]
    public class ScheduleReportController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ScheduledReportDto>(mediator, logger)
    {
        /// <summary>
        /// Arrange for a report to be sent on a schedule
        /// </summary>
        /// <remarks>
        /// The cron expression is shape checked here rather than left for the dispatcher to choke
        /// on, because a schedule that silently never fires is worse than one that is refused:
        /// nobody notices a report they were never getting.
        /// </remarks>
        [HttpPost(Name = "ScheduleReport")]
        public async Task<IActionResult> Index([FromBody] ScheduleReportDto dto)
        {
            var command = new ScheduleReportCommand
            {
                ReportTemplateId = dto.ReportTemplateId,
                CronExpression = dto.CronExpression,
                Recipients = dto.Recipients,
                IsActive = dto.IsActive
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
