using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RegisterWarrantyClaim;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.AfterSalesServiceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/WarrantyClaim/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AfterSalesSwaggerGroup)]
    public class RegisterWarrantyClaimController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<WarrantyClaimDto>(mediator, logger)
    {
        /// <summary>
        /// Record a warranty claim
        /// </summary>
        /// <remarks>
        /// Whether the claim is good is decided separately. Registering it starts the clock on the
        /// customer wait when they call, not when somebody gets round to looking at it.
        /// </remarks>
        [HttpPost(Name = "RegisterWarrantyClaim")]
        public async Task<IActionResult> Index([FromBody] RegisterWarrantyClaimDto dto)
        {
            var command = new RegisterWarrantyClaimCommand
            {
                CustomerId = dto.CustomerId,
                ItemId = dto.ItemId,
                FaultOccurredOn = dto.FaultOccurredOn,
                ReportedOn = dto.ReportedOn,
                FaultDescription = dto.FaultDescription,
                SerialNumberId = dto.SerialNumberId,
                ItemBatchId = dto.ItemBatchId,
                InvoiceId = dto.InvoiceId,
                BranchId = dto.BranchId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
