using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;
using ERP.Application.UseCases.Modules.ProjectManagement.Queries.GetProjectStatement;

namespace ERP.WebApi.Controllers.Modules.ProjectManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Project/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ProjectSwaggerGroup)]
    public class GetProjectStatementController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ProjectStatementDto>(mediator, logger)
    {
        /// <summary>
        /// What a project has cost, earned and still has to invoice
        /// </summary>
        /// <remarks>
        /// Only approved hours are counted. The window defaults to the project's own planned
        /// period, which is what somebody means when they ask how it is doing.
        /// </remarks>
        [HttpGet("{projectId:guid}", Name = "GetProjectStatement")]
        public async Task<IActionResult> Index(Guid projectId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            var query = new GetProjectStatementQuery
            {
                ProjectId = projectId,
                From = from,
                To = to
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
