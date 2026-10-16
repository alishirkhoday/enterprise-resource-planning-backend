using ERP.Application.UseCases.Modules.WarehouseManagement.Commands.DispatchStockTransfer;
using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.WarehouseManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/StockTransfer/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.WarehouseSwaggerGroup)]
    public class DispatchStockTransferController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<StockTransferDto>(mediator, logger)
    {
        /// <summary>
        /// Record goods leaving the source warehouse
        /// </summary>
        /// <remarks>
        /// The stock comes off the source's books here and reaches the destination's only when it
        /// is received, so in between it belongs to neither and is visible on the transfer alone.
        /// Naming no lines dispatches the whole transfer.
        /// </remarks>
        [HttpPost(Name = "DispatchStockTransfer")]
        public async Task<IActionResult> Index([FromBody] DispatchStockTransferDto dto)
        {
            var command = new DispatchStockTransferCommand
            {
                TransferId = dto.TransferId,
                DispatchDate = dto.DispatchDate,
                Lines = dto.Lines,
                VehicleNumber = dto.VehicleNumber
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
