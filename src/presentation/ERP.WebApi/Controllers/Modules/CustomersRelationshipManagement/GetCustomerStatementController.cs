using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Queries.GetCustomerStatement;

namespace ERP.WebApi.Controllers.Modules.CustomersRelationshipManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Invoice/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.InvoiceSwaggerGroup)]
    public class GetCustomerStatementController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<CustomerStatementDto>(mediator, logger)
    {
        /// <summary>
        /// What a customer was invoiced and still owes, aged
        /// </summary>
        /// <remarks>
        /// Drafts and cancelled invoices are left off, because a statement is meant to be
        /// reconciled against the customer's own records. The ageing is measured from today unless
        /// another date is given.
        /// </remarks>
        [HttpGet("{customerId:guid}", Name = "GetCustomerStatement")]
        public async Task<IActionResult> Index(Guid customerId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] DateOnly? asOf)
        {
            var query = new GetCustomerStatementQuery
            {
                CustomerId = customerId,
                From = from,
                To = to,
                AsOf = asOf
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
