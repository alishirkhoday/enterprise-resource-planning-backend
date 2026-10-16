using ERP.Application.UseCases.Modules.WarehouseManagement.Commands.CreateStockTransfer;
using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.WarehouseManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/StockTransfer/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.WarehouseSwaggerGroup)]
    public class CreateStockTransferController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<StockTransferDto>(mediator, logger)
    {
        /// <summary>
        /// Raise a transfer between two warehouses
        /// </summary>
        /// <remarks>
        /// Nothing moves yet. A line that omits its cost takes what the source warehouse is
        /// already carrying the stock at, because a transfer must not change what goods are worth.
        /// </remarks>
        [HttpPost(Name = "CreateStockTransfer")]
        public async Task<IActionResult> Index([FromBody] CreateStockTransferDto dto)
        {
            var command = new CreateStockTransferCommand
            {
                SourceWarehouseId = dto.SourceWarehouseId,
                DestinationWarehouseId = dto.DestinationWarehouseId,
                RequestDate = dto.RequestDate,
                CurrencyCode = dto.CurrencyCode,
                Lines = dto.Lines,
                ExpectedArrivalDate = dto.ExpectedArrivalDate,
                RequestedByHumanId = dto.RequestedByHumanId,
                VehicleNumber = dto.VehicleNumber,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
