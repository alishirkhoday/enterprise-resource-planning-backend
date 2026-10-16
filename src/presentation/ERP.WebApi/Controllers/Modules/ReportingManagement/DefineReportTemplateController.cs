using ERP.Application.UseCases.Modules.ReportingManagement.Commands.DefineReportTemplate;
using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ReportingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Reporting/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ReportingSwaggerGroup)]
    public class DefineReportTemplateController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ReportTemplateDto>(mediator, logger)
    {
        /// <summary>
        /// Register a report definition
        /// </summary>
        /// <remarks>
        /// The query is stored, never executed by this application. There is deliberately no
        /// endpoint that runs one: executing a caller-supplied query would be remote code
        /// execution against the database. A reporting service runs these under its own
        /// read-only credentials.
        /// </remarks>
        [HttpPost(Name = "DefineReportTemplate")]
        public async Task<IActionResult> Index([FromBody] DefineReportTemplateDto dto)
        {
            var command = new DefineReportTemplateCommand
            {
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category,
                QueryDefinition = dto.QueryDefinition,
                OutputFormat = dto.OutputFormat,
                Filters = dto.Filters
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
