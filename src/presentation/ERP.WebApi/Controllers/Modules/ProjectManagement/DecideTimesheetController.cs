using ERP.Application.UseCases.Modules.ProjectManagement.Commands.DecideTimesheet;
using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ProjectManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Timesheet/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ProjectSwaggerGroup)]
    public class DecideTimesheetController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<TimesheetDto>(mediator, logger)
    {
        /// <summary>
        /// Approve or reject a submitted timesheet
        /// </summary>
        /// <remarks>
        /// Approval is the gate between what somebody says they did and what the business bills
        /// for: only approved hours reach a project cost.
        /// </remarks>
        [HttpPost("{timesheetId:guid}", Name = "DecideTimesheet")]
        public async Task<IActionResult> Index(Guid timesheetId, [FromBody] DecideTimesheetRequest body)
        {
            var command = new DecideTimesheetCommand
            {
                TimesheetId = timesheetId,
                IsApproved = body.IsApproved,
                ApprovedByHumanId = body.ApprovedByHumanId,
                Reason = body.Reason
            };

            return FromResult(await _mediator.Send(command));
        }

        public sealed record DecideTimesheetRequest(bool IsApproved, Guid ApprovedByHumanId, string? Reason);
    }
}
