using ERP.Application.UseCases.Modules.PlanningManagement.Commands.ReleasePlannedOrder;
using ERP.Application.UseCases.Modules.PlanningManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.PlanningManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Planning/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PlanningSwaggerGroup)]
    public class ReleasePlannedOrderController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ReleasePlannedOrderResultDto>(mediator, logger)
    {
        /// <summary>
        /// Turn planning suggestions into work somebody owns
        /// </summary>
        /// <remarks>
        /// A released line is marked as such, so the same shortage cannot become two orders when
        /// the next person opens the plan.
        /// </remarks>
        [HttpPost(Name = "ReleasePlannedOrder")]
        public async Task<IActionResult> Index([FromBody] ReleasePlannedOrderDto dto)
        {
            var command = new ReleasePlannedOrderCommand
            {
                PlanId = dto.PlanId,
                LineIds = dto.LineIds
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
