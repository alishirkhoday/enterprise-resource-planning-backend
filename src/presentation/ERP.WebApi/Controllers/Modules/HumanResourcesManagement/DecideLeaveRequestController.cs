using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.DecideLeaveRequest;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.HumanResourcesManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Human/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.HumanSwaggerGroup)]
    public class DecideLeaveRequestController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<LeaveRequestDto>(mediator, logger)
    {
        /// <summary>
        /// Approve or refuse a leave request
        /// </summary>
        /// <remarks>
        /// Approval turns the held days into used days; a refusal gives them back. Either way the
        /// reservation is settled, so nothing stays held against leave nobody will take.
        /// </remarks>
        [HttpPost("{requestId:guid}", Name = "DecideLeaveRequest")]
        public async Task<IActionResult> Index(Guid requestId, [FromBody] DecideLeaveRequestRequest body)
        {
            var command = new DecideLeaveRequestCommand
            {
                RequestId = requestId,
                IsApproved = body.IsApproved,
                Reason = body.Reason
            };

            return FromResult(await _mediator.Send(command));
        }

        public sealed record DecideLeaveRequestRequest(bool IsApproved, string? Reason);
    }
}
