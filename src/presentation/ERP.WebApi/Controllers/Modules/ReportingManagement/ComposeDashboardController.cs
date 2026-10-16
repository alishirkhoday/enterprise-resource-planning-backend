using ERP.Application.UseCases.Modules.ReportingManagement.Commands.ComposeDashboard;
using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ReportingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Reporting/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ReportingSwaggerGroup)]
    public class ComposeDashboardController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<DashboardDto>(mediator, logger)
    {
        /// <summary>
        /// Assemble a dashboard out of report templates
        /// </summary>
        /// <remarks>
        /// Every widget has to name a template that exists, because a widget pointing at nothing
        /// renders as a broken tile nobody can explain. Two widgets cannot share a display order.
        /// </remarks>
        [HttpPost(Name = "ComposeDashboard")]
        public async Task<IActionResult> Index([FromBody] ComposeDashboardDto dto)
        {
            var command = new ComposeDashboardCommand
            {
                Title = dto.Title,
                Description = dto.Description,
                Widgets = dto.Widgets
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
