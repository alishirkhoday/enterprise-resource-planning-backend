using ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ApproveStockTransfer;
using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.WarehouseManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/StockTransfer/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.WarehouseSwaggerGroup)]
    public class ApproveStockTransferController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<StockTransferDto>(mediator, logger)
    {
        /// <summary>
        /// Approve a transfer request
        /// </summary>
        /// <remarks>
        /// Approval is permission to dispatch. The stock only leaves the source when somebody
        /// records that it did.
        /// </remarks>
        [HttpPost(Name = "ApproveStockTransfer")]
        public async Task<IActionResult> Index([FromBody] ApproveStockTransferDto dto)
        {
            var command = new ApproveStockTransferCommand
            {
                TransferId = dto.TransferId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
