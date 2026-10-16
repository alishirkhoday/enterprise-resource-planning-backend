using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ReviseQuotation;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Quotation/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.QuotationSwaggerGroup)]
    public class ReviseQuotationController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<QuotationDto>(mediator, logger)
    {
        /// <summary>
        /// Open a new version of an offer after the customer pushed back
        /// </summary>
        /// <remarks>
        /// The version moves on the same document and it goes back out, so the link to the deal
        /// and the number the customer is holding both survive. Naming no lines carries the
        /// previous ones across; the validity window restarts from today either way.
        /// </remarks>
        [HttpPost(Name = "ReviseQuotation")]
        public async Task<IActionResult> Index([FromBody] ReviseQuotationDto dto)
        {
            var command = new ReviseQuotationCommand
            {
                QuotationId = dto.QuotationId,
                Lines = dto.Lines,
                ValidForDays = dto.ValidForDays,
                HeaderDiscountPercentage = dto.HeaderDiscountPercentage,
                Reason = dto.Reason
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
