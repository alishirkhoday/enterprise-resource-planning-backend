using ERP.Application.UseCases.Modules.ProjectManagement.Commands.SubmitTimesheet;
using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ProjectManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Timesheet/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ProjectSwaggerGroup)]
    public class SubmitTimesheetController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<TimesheetDto>(mediator, logger)
    {
        /// <summary>
        /// Fill in a timesheet and submit it
        /// </summary>
        /// <remarks>
        /// Resubmitting a period replaces what was there rather than adding to it. Time booked
        /// without a task is internal and never billable, whatever the request says.
        /// </remarks>
        [HttpPost(Name = "SubmitTimesheet")]
        public async Task<IActionResult> Index([FromBody] SubmitTimesheetDto dto)
        {
            var command = new SubmitTimesheetCommand
            {
                HumanId = dto.HumanId,
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                Entries = dto.Entries
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
