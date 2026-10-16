using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Queries.GetAttendanceSummary;

namespace ERP.WebApi.Controllers.Modules.HumanResourcesManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Human/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.HumanSwaggerGroup)]
    public class GetAttendanceSummaryController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<AttendanceSummaryDto>(mediator, logger)
    {
        /// <summary>
        /// What one person's attendance looks like over a period
        /// </summary>
        /// <remarks>
        /// The days come back, not just a total: this is the figure payroll pays against and the
        /// one an employee disputes. A day the clock saw somebody arrive but never leave reports
        /// no hours and is counted separately rather than silently skipped.
        /// </remarks>
        [HttpGet("{humanId:guid}", Name = "GetAttendanceSummary")]
        public async Task<IActionResult> Index(Guid humanId, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var query = new GetAttendanceSummaryQuery
            {
                HumanId = humanId,
                From = from,
                To = to
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
