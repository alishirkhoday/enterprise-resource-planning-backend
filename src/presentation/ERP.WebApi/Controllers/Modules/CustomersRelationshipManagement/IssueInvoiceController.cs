using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.IssueInvoice;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.CustomersRelationshipManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Invoice/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.InvoiceSwaggerGroup)]
    public class IssueInvoiceController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<InvoiceDto>(mediator, logger)
    {
        /// <summary>
        /// Issue a sales invoice
        /// </summary>
        /// <remarks>
        /// A line that omits its price is priced from the list in effect for the customer, which
        /// is the only way an agreed price cannot be quietly undercut. The credit limit is checked
        /// here rather than at order time, because an order can sit for weeks while the customer's
        /// position moves.
        /// </remarks>
        [HttpPost(Name = "IssueInvoice")]
        public async Task<IActionResult> Index([FromBody] IssueInvoiceDto dto)
        {
            var command = new IssueInvoiceCommand
            {
                CustomerId = dto.CustomerId,
                IssueDate = dto.IssueDate,
                DueDate = dto.DueDate,
                CurrencyCode = dto.CurrencyCode,
                Lines = dto.Lines,
                SaleOrderId = dto.SaleOrderId,
                PaymentTermId = dto.PaymentTermId,
                SalesRepHumanId = dto.SalesRepHumanId,
                BranchId = dto.BranchId,
                AdditionalCharges = dto.AdditionalCharges,
                HeaderDiscountPercentage = dto.HeaderDiscountPercentage,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
