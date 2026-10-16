using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.RecordCampaignCost;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Campaign/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.CampaignSwaggerGroup)]
    public class RecordCampaignCostController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<CampaignDto>(mediator, logger)
    {
        /// <summary>
        /// Book spend against a campaign
        /// </summary>
        /// <remarks>
        /// Costs go in as they are incurred, because the number worth having is the one available
        /// while the campaign can still be stopped. Overspending is recorded, not refused: the
        /// money is already gone and hiding it helps nobody.
        /// </remarks>
        [HttpPost(Name = "RecordCampaignCost")]
        public async Task<IActionResult> Index([FromBody] RecordCampaignCostDto dto)
        {
            var command = new RecordCampaignCostCommand
            {
                CampaignId = dto.CampaignId,
                Amount = dto.Amount,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
