using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Application.UseCases.Modules.InventoryManagement.Queries.GetItemStockCard;

namespace ERP.WebApi.Controllers.Modules.InventoryManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Inventory/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.WarehouseSwaggerGroup)]
    public class GetItemStockCardController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ItemStockCardDto>(mediator, logger)
    {
        /// <summary>
        /// Every movement of an item in a warehouse, with its running balance
        /// </summary>
        /// <remarks>
        /// The opening balance is read from the ledger as it stood before the window rather than
        /// derived from the closing one, so a card that does not add up will say so instead of
        /// quietly balancing. The window defaults to the last month.
        /// </remarks>
        [HttpGet("{itemId:guid}/{warehouseId:guid}", Name = "GetItemStockCard")]
        public async Task<IActionResult> Index(Guid itemId, Guid warehouseId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            var query = new GetItemStockCardQuery
            {
                ItemId = itemId,
                WarehouseId = warehouseId,
                From = from,
                To = to
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
