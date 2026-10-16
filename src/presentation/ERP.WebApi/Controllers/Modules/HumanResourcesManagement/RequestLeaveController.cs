using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RequestLeave;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.HumanResourcesManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Human/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.HumanSwaggerGroup)]
    public class RequestLeaveController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<LeaveRequestDto>(mediator, logger)
    {
        /// <summary>
        /// Ask for leave
        /// </summary>
        /// <remarks>
        /// The days are held against the entitlement immediately, not on approval. Between asking
        /// and being answered they are neither free nor spent, and counting only approved leave
        /// lets the same week be booked three times.
        /// </remarks>
        [HttpPost(Name = "RequestLeave")]
        public async Task<IActionResult> Index([FromBody] RequestLeaveDto dto)
        {
            var command = new RequestLeaveCommand
            {
                HumanId = dto.HumanId,
                Type = dto.Type,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                LeaveType = dto.LeaveType,
                Reason = dto.Reason
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
