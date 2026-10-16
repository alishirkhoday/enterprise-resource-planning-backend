using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.IssueQuotation;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Quotation/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.QuotationSwaggerGroup)]
    public class IssueQuotationController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<QuotationDto>(mediator, logger)
    {
        /// <summary>
        /// Issue a quotation and send it
        /// </summary>
        /// <remarks>
        /// Prices fall back to the same list an invoice will use, so the two never disagree in
        /// front of the customer. The validity window is not optional: without it a price quoted
        /// a year ago is still, on paper, a price you offered.
        /// </remarks>
        [HttpPost(Name = "IssueQuotation")]
        public async Task<IActionResult> Index([FromBody] IssueQuotationDto dto)
        {
            var command = new IssueQuotationCommand
            {
                CustomerId = dto.CustomerId,
                CurrencyCode = dto.CurrencyCode,
                Lines = dto.Lines,
                ValidForDays = dto.ValidForDays,
                OpportunityId = dto.OpportunityId,
                PaymentTermId = dto.PaymentTermId,
                PriceListId = dto.PriceListId,
                SalesRepHumanId = dto.SalesRepHumanId,
                BranchId = dto.BranchId,
                AdditionalCharges = dto.AdditionalCharges,
                HeaderDiscountPercentage = dto.HeaderDiscountPercentage,
                Terms = dto.Terms,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
