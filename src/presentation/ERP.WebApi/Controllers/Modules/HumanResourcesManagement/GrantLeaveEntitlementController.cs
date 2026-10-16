using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.GrantLeaveEntitlement;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.HumanResourcesManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Human/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.HumanSwaggerGroup)]
    public class GrantLeaveEntitlementController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<LeaveEntitlementDto>(mediator, logger)
    {
        /// <summary>
        /// Open an employee's leave balance for a year
        /// </summary>
        /// <remarks>
        /// This is what a leave request checks itself against. An existing balance is restated
        /// rather than replaced, so days already used or held stay where they are: wiping them
        /// would hand back leave somebody has already taken.
        /// </remarks>
        [HttpPost(Name = "GrantLeaveEntitlement")]
        public async Task<IActionResult> Index([FromBody] GrantLeaveEntitlementDto dto)
        {
            var command = new GrantLeaveEntitlementCommand
            {
                HumanId = dto.HumanId,
                LeaveType = dto.LeaveType,
                Year = dto.Year,
                EntitledDays = dto.EntitledDays,
                CarriedOverDays = dto.CarriedOverDays
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
