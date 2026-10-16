using ERP.Application.UseCases.Modules.ProjectManagement.Commands.AchieveMilestone;
using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ProjectManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Project/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ProjectSwaggerGroup)]
    public class AchieveMilestoneController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ProjectMilestoneDto>(mediator, logger)
    {
        /// <summary>
        /// Record that a milestone has been reached
        /// </summary>
        /// <remarks>
        /// On a milestone that carries a value this is what makes it billable. Whether it counts
        /// as late is judged against its own due date, not asserted by the caller.
        /// </remarks>
        [HttpPost("{milestoneId:guid}", Name = "AchieveMilestone")]
        public async Task<IActionResult> Index(Guid milestoneId, [FromBody] AchieveMilestoneRequest body)
        {
            var command = new AchieveMilestoneCommand
            {
                MilestoneId = milestoneId,
                AchievedOn = body.AchievedOn
            };

            return FromResult(await _mediator.Send(command));
        }

        public sealed record AchieveMilestoneRequest(DateOnly AchievedOn);
    }
}
