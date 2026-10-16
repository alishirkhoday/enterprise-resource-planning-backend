using ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreateStockCount;
using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.InventoryManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/StockCount/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.StockCountSwaggerGroup)]
    public class CreateStockCountController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<StockCountDto>(mediator, logger)
    {
        /// <summary>
        /// Open a count sheet and freeze the book quantities
        /// </summary>
        /// <remarks>
        /// The sheet is built and frozen together on purpose: any gap between capturing what the
        /// books said and declaring that the figure being counted against is a window in which
        /// stock moves, and every variance from it would be a lie about what the shelf held.
        /// </remarks>
        [HttpPost(Name = "CreateStockCount")]
        public async Task<IActionResult> Index([FromBody] CreateStockCountDto dto)
        {
            var command = new CreateStockCountCommand
            {
                WarehouseId = dto.WarehouseId,
                Type = dto.Type,
                CountDate = dto.CountDate,
                ItemIds = dto.ItemIds,
                StorageLocationId = dto.StorageLocationId,
                CountedByHumanId = dto.CountedByHumanId,
                BranchId = dto.BranchId,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
