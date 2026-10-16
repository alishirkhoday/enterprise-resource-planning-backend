using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetSalesPipeline;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Opportunity/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.OpportunitySwaggerGroup)]
    public class GetSalesPipelineController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<SalesPipelineDto>(mediator, logger)
    {
        /// <summary>
        /// The open pipeline by stage, with its weighted value
        /// </summary>
        /// <remarks>
        /// The weighted value sits next to the raw estimate on purpose: the gap between them is
        /// how much of the pipeline is hope. Deals in other currencies are left out rather than
        /// converted at a rate nobody supplied.
        /// </remarks>
        [HttpGet(Name = "GetSalesPipeline")]
        public async Task<IActionResult> Index([FromQuery] Guid? assignedToHumanId, [FromQuery] string? currencyCode, [FromQuery] DateOnly? asOf)
        {
            var query = new GetSalesPipelineQuery
            {
                AssignedToHumanId = assignedToHumanId,
                CurrencyCode = currencyCode,
                AsOf = asOf
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
