using ERP.Application.UseCases.Modules.InventoryManagement.Commands.RecordStockCount;
using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.InventoryManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/StockCount/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.StockCountSwaggerGroup)]
    public class RecordStockCountController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<StockCountDto>(mediator, logger)
    {
        /// <summary>
        /// Record what was found on the shelves
        /// </summary>
        /// <remarks>
        /// Nothing about the books changes here. The sheet can be filled in over several
        /// requests, and SubmitWhenComplete sends it for approval as soon as no line is left
        /// blank; the status on the response says whether that happened.
        /// </remarks>
        [HttpPost(Name = "RecordStockCount")]
        public async Task<IActionResult> Index([FromBody] RecordStockCountDto dto)
        {
            var command = new RecordStockCountCommand
            {
                StockCountId = dto.StockCountId,
                Lines = dto.Lines,
                SubmitWhenComplete = dto.SubmitWhenComplete
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
