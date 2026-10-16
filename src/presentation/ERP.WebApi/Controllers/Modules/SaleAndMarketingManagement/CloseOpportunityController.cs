using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CloseOpportunity;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Opportunity/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.OpportunitySwaggerGroup)]
    public class CloseOpportunityController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<OpportunityDto>(mediator, logger)
    {
        /// <summary>
        /// Close a deal, won or lost
        /// </summary>
        /// <remarks>
        /// A win posts against every sales target covering the salesperson on that date and
        /// against the campaign that brought the deal in, so a scoreboard is never a day behind
        /// the room. A loss needs its reason: lost reasons are the only free lesson here.
        /// </remarks>
        [HttpPost(Name = "CloseOpportunity")]
        public async Task<IActionResult> Index([FromBody] CloseOpportunityDto dto)
        {
            var command = new CloseOpportunityCommand
            {
                OpportunityId = dto.OpportunityId,
                IsWon = dto.IsWon,
                ClosedOn = dto.ClosedOn,
                LostReason = dto.LostReason,
                SaleOrderId = dto.SaleOrderId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
