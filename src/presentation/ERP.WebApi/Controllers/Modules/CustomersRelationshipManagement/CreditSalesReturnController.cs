using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CreditSalesReturn;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.CustomersRelationshipManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Invoice/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.InvoiceSwaggerGroup)]
    public class CreditSalesReturnController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<InvoiceDto>(mediator, logger)
    {
        /// <summary>
        /// Credit a sale that came back
        /// </summary>
        /// <remarks>
        /// The credit note is a new document rather than an edit to the original, which has
        /// already been filed with the tax authority. Lines are credited at the price and tax they
        /// were sold at, so the customer gets back what they paid. Naming no lines credits the
        /// whole invoice.
        /// </remarks>
        [HttpPost(Name = "CreditSalesReturn")]
        public async Task<IActionResult> Index([FromBody] CreditSalesReturnDto dto)
        {
            var command = new CreditSalesReturnCommand
            {
                OriginalInvoiceId = dto.OriginalInvoiceId,
                IssueDate = dto.IssueDate,
                Reason = dto.Reason,
                Lines = dto.Lines,
                WarehouseId = dto.WarehouseId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
