using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.DecideQuotation;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Quotation/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.QuotationSwaggerGroup)]
    public class DecideQuotationController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<QuotationDto>(mediator, logger)
    {
        /// <summary>
        /// Record the customer's answer to a quotation
        /// </summary>
        /// <remarks>
        /// An expired quotation cannot be accepted - honouring one past its expiry means the
        /// expiry never meant anything. A rejection is still recorded on a lapsed quotation,
        /// because the reason is worth keeping. Accepting wins the linked deal.
        /// </remarks>
        [HttpPost(Name = "DecideQuotation")]
        public async Task<IActionResult> Index([FromBody] DecideQuotationDto dto)
        {
            var command = new DecideQuotationCommand
            {
                QuotationId = dto.QuotationId,
                IsAccepted = dto.IsAccepted,
                RejectionReason = dto.RejectionReason,
                SaleOrderId = dto.SaleOrderId,
                CloseOpportunity = dto.CloseOpportunity
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
