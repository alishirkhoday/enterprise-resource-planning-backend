using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CompleteActivity;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/SalesActivity/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.SalesActivitySwaggerGroup)]
    public class CompleteActivityController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<CompleteActivityResultDto>(mediator, logger)
    {
        /// <summary>
        /// Record what came of an activity, and book the next one
        /// </summary>
        /// <remarks>
        /// The follow up is part of the same request because that is the moment it gets decided.
        /// A salesperson who has to open a second screen books it less often, and a deal with no
        /// next step is how a pipeline goes quiet without anybody noticing.
        /// </remarks>
        [HttpPost(Name = "CompleteActivity")]
        public async Task<IActionResult> Index([FromBody] CompleteActivityDto dto)
        {
            var command = new CompleteActivityCommand
            {
                ActivityId = dto.ActivityId,
                Outcome = dto.Outcome,
                DurationMinutes = dto.DurationMinutes,
                FollowUp = dto.FollowUp
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
