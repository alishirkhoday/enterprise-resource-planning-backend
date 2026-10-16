using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CreateOpportunity;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Opportunity/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.OpportunitySwaggerGroup)]
    public class CreateOpportunityController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<OpportunityDto>(mediator, logger)
    {
        /// <summary>
        /// Open a deal
        /// </summary>
        /// <remarks>
        /// A deal has to belong to a customer or a lead. One that belongs to neither cannot be
        /// chased and still counts toward the forecast, which is how a pipeline predicts revenue
        /// from nobody.
        /// </remarks>
        [HttpPost(Name = "CreateOpportunity")]
        public async Task<IActionResult> Index([FromBody] CreateOpportunityDto dto)
        {
            var command = new CreateOpportunityCommand
            {
                Title = dto.Title,
                EstimatedValue = dto.EstimatedValue,
                CurrencyCode = dto.CurrencyCode,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                CustomerId = dto.CustomerId,
                SourceLeadId = dto.SourceLeadId,
                CampaignId = dto.CampaignId,
                AssignedToHumanId = dto.AssignedToHumanId,
                BranchId = dto.BranchId,
                CompetitorName = dto.CompetitorName,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
