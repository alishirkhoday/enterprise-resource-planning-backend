using ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreatePurchaseOrder;
using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.InventoryManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/PurchaseOrder/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PurchasingSwaggerGroup)]
    public class CreatePurchaseOrderController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<PurchaseOrderDto>(mediator, logger)
    {
        /// <summary>
        /// Raise a purchase order
        /// </summary>
        /// <remarks>
        /// A line without a price falls back to the framework agreement with the supplier. A line
        /// with neither is refused rather than ordered at nothing.
        /// </remarks>
        [HttpPost(Name = "CreatePurchaseOrder")]
        public async Task<IActionResult> Index([FromBody] CreatePurchaseOrderDto dto)
        {
            var command = new CreatePurchaseOrderCommand
            {
                SupplierId = dto.SupplierId,
                CurrencyCode = dto.CurrencyCode,
                Lines = dto.Lines,
                ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                WarehouseId = dto.WarehouseId,
                PaymentTermId = dto.PaymentTermId,
                CostCenterId = dto.CostCenterId,
                BranchId = dto.BranchId,
                AdditionalCosts = dto.AdditionalCosts,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
