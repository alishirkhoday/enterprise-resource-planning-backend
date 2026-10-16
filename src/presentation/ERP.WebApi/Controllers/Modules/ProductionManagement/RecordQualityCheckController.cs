using ERP.Application.UseCases.Modules.ProductionManagement.Commands.RecordQualityCheck;
using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ProductionManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Quality/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.QualitySwaggerGroup)]
    public class RecordQualityCheckController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<QualityCheckDto>(mediator, logger)
    {
        /// <summary>
        /// Record an inspection and what it measured
        /// </summary>
        /// <remarks>
        /// The readings are kept, not only the verdict: a verdict says this batch was wrong, a run
        /// of readings creeping toward a tolerance says the process will be wrong next week. A
        /// numeric reading is judged against its parameter and cannot be talked into passing.
        /// </remarks>
        [HttpPost(Name = "RecordQualityCheck")]
        public async Task<IActionResult> Index([FromBody] RecordQualityCheckDto dto)
        {
            var command = new RecordQualityCheckCommand
            {
                InspectorHumanId = dto.InspectorHumanId,
                InspectedQuantity = dto.InspectedQuantity,
                PassedQuantity = dto.PassedQuantity,
                Measurements = dto.Measurements,
                ProductionTaskId = dto.ProductionTaskId,
                InspectionPlanId = dto.InspectionPlanId,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                Notes = dto.Notes
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
