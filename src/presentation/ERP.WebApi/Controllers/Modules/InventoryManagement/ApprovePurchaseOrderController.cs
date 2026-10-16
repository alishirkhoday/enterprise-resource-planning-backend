using ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApprovePurchaseOrder;
using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.InventoryManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/PurchaseOrder/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PurchasingSwaggerGroup)]
    public class ApprovePurchaseOrderController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<PurchaseOrderDto>(mediator, logger)
    {
        /// <summary>
        /// Approve or reject a submitted order
        /// </summary>
        /// <remarks>A rejection needs a reason; an approval does not.</remarks>
        [HttpPost("{purchaseOrderId:guid}", Name = "ApprovePurchaseOrder")]
        public async Task<IActionResult> Index(Guid purchaseOrderId, [FromBody] ApprovePurchaseOrderRequest body)
        {
            var command = new ApprovePurchaseOrderCommand
            {
                PurchaseOrderId = purchaseOrderId,
                IsApproved = body.IsApproved,
                Reason = body.Reason,
                ApprovalRequestId = body.ApprovalRequestId
            };

            return FromResult(await _mediator.Send(command));
        }

        public sealed record ApprovePurchaseOrderRequest(bool IsApproved, string? Reason, Guid? ApprovalRequestId);
    }
}
