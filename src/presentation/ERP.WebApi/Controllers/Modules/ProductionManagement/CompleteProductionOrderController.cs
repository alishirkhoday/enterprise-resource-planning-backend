using ERP.Application.UseCases.Modules.ProductionManagement.Commands.CompleteProductionOrder;
using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ProductionManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Production/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ProductionSwaggerGroup)]
    public class CompleteProductionOrderController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ProductionOrderDto>(mediator, logger)
    {
        /// <summary>
        /// Close a works order and receive what it made
        /// </summary>
        /// <remarks>
        /// The finished goods are valued at what the run cost - material, labour and overhead -
        /// over the good output alone. Scrap is costed but never becomes stock, which is what
        /// makes a bad run show up as an expensive product rather than disappearing.
        /// </remarks>
        [HttpPost(Name = "CompleteProductionOrder")]
        public async Task<IActionResult> Index([FromBody] CompleteProductionOrderDto dto)
        {
            var command = new CompleteProductionOrderCommand
            {
                ProductionOrderId = dto.ProductionOrderId,
                ProducedQuantity = dto.ProducedQuantity,
                ScrappedQuantity = dto.ScrappedQuantity,
                WarehouseId = dto.WarehouseId,
                StorageLocationId = dto.StorageLocationId,
                ItemBatchId = dto.ItemBatchId,
                LabourCost = dto.LabourCost,
                OverheadCost = dto.OverheadCost,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
