using ERP.Application.UseCases.Modules.ProductionManagement.Commands.ReleaseProductionOrder;
using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ProductionManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Production/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ProductionSwaggerGroup)]
    public class ReleaseProductionOrderController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ProductionOrderDto>(mediator, logger)
    {
        /// <summary>
        /// Hand a planned order to the shop floor
        /// </summary>
        /// <remarks>
        /// Releasing is what makes material issuable against the order, so it is the moment the
        /// rest of the business starts paying for it.
        /// </remarks>
        [HttpPost(Name = "ReleaseProductionOrder")]
        public async Task<IActionResult> Index([FromBody] ReleaseProductionOrderDto dto)
        {
            var command = new ReleaseProductionOrderCommand
            {
                ProductionOrderId = dto.ProductionOrderId,
                StartImmediately = dto.StartImmediately
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
