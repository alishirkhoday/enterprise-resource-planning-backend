using ERP.Application.UseCases.Modules.PricingManagement.DTOs;
using ERP.Application.UseCases.Modules.PricingManagement.Queries.GetEffectivePrice;

namespace ERP.WebApi.Controllers.Modules.PricingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/PriceList/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PriceListSwaggerGroup)]
    public class GetEffectivePriceController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<EffectivePriceDto>(mediator, logger)
    {
        /// <summary>
        /// What an item costs a given customer today
        /// </summary>
        /// <remarks>
        /// Resolved exactly as an invoice will - the customer's own list beats their group's,
        /// which beats the default, and the quantity picks the volume break - so a quote and the
        /// bill that follows it cannot disagree.
        /// </remarks>
        [HttpGet("{itemId:guid}", Name = "GetEffectivePrice")]
        public async Task<IActionResult> Index(
            Guid itemId,
            [FromQuery] decimal quantity = 1m,
            [FromQuery] Guid? customerId = null,
            [FromQuery] Guid? priceListId = null,
            [FromQuery] string? currencyCode = null,
            [FromQuery] DateOnly? onDate = null)
        {
            var query = new GetEffectivePriceQuery
            {
                ItemId = itemId,
                Quantity = quantity,
                CustomerId = customerId,
                PriceListId = priceListId,
                CurrencyCode = currencyCode,
                OnDate = onDate
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
