using ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ReceiveStockTransfer;
using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.WarehouseManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/StockTransfer/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.WarehouseSwaggerGroup)]
    public class ReceiveStockTransferController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<StockTransferDto>(mediator, logger)
    {
        /// <summary>
        /// Record goods arriving at the destination
        /// </summary>
        /// <remarks>
        /// What arrives is valued at what left, so a transfer cannot make a margin appear. A line
        /// that arrived short needs a reason, and the missing stock is deliberately not written
        /// off here: that is a decision, not a side effect of receiving the rest of the pallet.
        /// </remarks>
        [HttpPost(Name = "ReceiveStockTransfer")]
        public async Task<IActionResult> Index([FromBody] ReceiveStockTransferDto dto)
        {
            var command = new ReceiveStockTransferCommand
            {
                TransferId = dto.TransferId,
                ReceiveDate = dto.ReceiveDate,
                Lines = dto.Lines
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
