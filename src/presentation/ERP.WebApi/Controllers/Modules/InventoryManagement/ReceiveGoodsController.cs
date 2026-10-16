using ERP.Application.UseCases.Modules.InventoryManagement.Commands.ReceiveGoods;
using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.InventoryManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/GoodsReceipt/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PurchasingSwaggerGroup)]
    public class ReceiveGoodsController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<GoodsReceiptDto>(mediator, logger)
    {
        /// <summary>
        /// Book a delivery in against a purchase order
        /// </summary>
        /// <remarks>
        /// The receipt, the order lines it satisfies and the stock it puts on the shelf all move
        /// together. Only what passes inspection becomes stock; a rejected quantity arrived but
        /// never enters inventory.
        /// </remarks>
        [HttpPost(Name = "ReceiveGoods")]
        public async Task<IActionResult> Index([FromBody] ReceiveGoodsDto dto)
        {
            var command = new ReceiveGoodsCommand
            {
                PurchaseOrderId = dto.PurchaseOrderId,
                WarehouseId = dto.WarehouseId,
                ReceiptDate = dto.ReceiptDate,
                Lines = dto.Lines,
                StorageLocationId = dto.StorageLocationId,
                ReceivedByHumanId = dto.ReceivedByHumanId,
                DeliveryNoteNumber = dto.DeliveryNoteNumber,
                VehicleNumber = dto.VehicleNumber,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
