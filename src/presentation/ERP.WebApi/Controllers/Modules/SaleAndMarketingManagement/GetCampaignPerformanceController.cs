using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetCampaignPerformance;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Campaign/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.CampaignSwaggerGroup)]
    public class GetCampaignPerformanceController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<CampaignPerformanceDto>(mediator, logger)
    {
        /// <summary>
        /// What a campaign cost and what it brought in
        /// </summary>
        /// <remarks>
        /// Cost per lead sits next to cost per conversion because the two often disagree, and the
        /// disagreement is the finding: cheap leads with expensive conversions means the campaign
        /// is buying the wrong people.
        /// </remarks>
        [HttpGet("{campaignId:guid}", Name = "GetCampaignPerformance")]
        public async Task<IActionResult> Index(Guid campaignId)
        {
            return FromResult(await _mediator.Send(new GetCampaignPerformanceQuery { CampaignId = campaignId }));
        }
    }
}
