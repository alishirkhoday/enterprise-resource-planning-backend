using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetSalesPerformance;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/SalesTarget/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.SalesTargetSwaggerGroup)]
    public class GetSalesPerformanceController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<SalesPerformanceDto>(mediator, logger)
    {
        /// <summary>
        /// How a salesperson is doing against target
        /// </summary>
        /// <remarks>
        /// Target, won, pipeline and commission come back together because each alone misleads:
        /// sixty percent of target with a full pipeline is a different position from sixty percent
        /// with nothing left to close.
        /// </remarks>
        [HttpGet("{salesRepHumanId:guid}", Name = "GetSalesPerformance")]
        public async Task<IActionResult> Index(Guid salesRepHumanId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            var query = new GetSalesPerformanceQuery
            {
                SalesRepHumanId = salesRepHumanId,
                From = from,
                To = to
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
