using ERP.Application.UseCases.Modules.PlanningManagement.Commands.RunMaterialRequirementPlan;
using ERP.Application.UseCases.Modules.PlanningManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.PlanningManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Planning/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PlanningSwaggerGroup)]
    public class RunMaterialRequirementPlanController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<MaterialRequirementPlanDto>(mediator, logger)
    {
        /// <summary>
        /// Run material requirements planning over a horizon
        /// </summary>
        /// <remarks>
        /// Nothing is committed. Each line is a suggestion until somebody releases it, and the
        /// response returns only the lines that are short of something.
        /// </remarks>
        [HttpPost(Name = "RunMaterialRequirementPlan")]
        public async Task<IActionResult> Index([FromBody] RunMaterialRequirementPlanDto dto)
        {
            var command = new RunMaterialRequirementPlanCommand
            {
                HorizonStart = dto.HorizonStart,
                HorizonEnd = dto.HorizonEnd,
                DemandForecastId = dto.DemandForecastId,
                WarehouseId = dto.WarehouseId,
                BranchId = dto.BranchId,
                ItemIds = dto.ItemIds,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
