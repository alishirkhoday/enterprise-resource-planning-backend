using ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApproveStockCount;
using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.InventoryManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/StockCount/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.StockCountSwaggerGroup)]
    public class ApproveStockCountController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<StockCountDto>(mediator, logger)
    {
        /// <summary>
        /// Approve a count and adjust the books to it
        /// </summary>
        /// <remarks>
        /// The one place stock changes without anything having physically moved, which is why it
        /// is separate and carries a name. Every adjustment leaves a ledger entry naming the count
        /// it came from, so a variance can always be traced back to the sheet that found it.
        /// </remarks>
        [HttpPost(Name = "ApproveStockCount")]
        public async Task<IActionResult> Index([FromBody] ApproveStockCountDto dto)
        {
            var command = new ApproveStockCountCommand
            {
                StockCountId = dto.StockCountId,
                ApprovedByHumanId = dto.ApprovedByHumanId,
                AdjustmentDocumentId = dto.AdjustmentDocumentId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
