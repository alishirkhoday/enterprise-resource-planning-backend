using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.AdvanceOpportunity;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Opportunity/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.OpportunitySwaggerGroup)]
    public class AdvanceOpportunityController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<OpportunityDto>(mediator, logger)
    {
        /// <summary>
        /// Move a deal along the pipeline
        /// </summary>
        /// <remarks>
        /// The stage resets the probability to the one that stage implies unless the request
        /// overrides it, which is what makes a forecast comparable between salespeople. Winning
        /// or losing is recorded by closing the deal, not by moving its stage.
        /// </remarks>
        [HttpPost(Name = "AdvanceOpportunity")]
        public async Task<IActionResult> Index([FromBody] AdvanceOpportunityDto dto)
        {
            var command = new AdvanceOpportunityCommand
            {
                OpportunityId = dto.OpportunityId,
                Stage = dto.Stage,
                Probability = dto.Probability,
                EstimatedValue = dto.EstimatedValue,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                CompetitorName = dto.CompetitorName
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
