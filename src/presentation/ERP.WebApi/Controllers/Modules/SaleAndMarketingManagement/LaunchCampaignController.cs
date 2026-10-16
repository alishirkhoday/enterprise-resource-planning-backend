using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.LaunchCampaign;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Campaign/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.CampaignSwaggerGroup)]
    public class LaunchCampaignController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<CampaignDto>(mediator, logger)
    {
        /// <summary>
        /// Set up a campaign and start it
        /// </summary>
        /// <remarks>
        /// The budget is what makes a campaign answerable afterwards. One that only counts leads
        /// always looks successful, because attracting the wrong people is cheap.
        /// </remarks>
        [HttpPost(Name = "LaunchCampaign")]
        public async Task<IActionResult> Index([FromBody] LaunchCampaignDto dto)
        {
            var command = new LaunchCampaignCommand
            {
                Code = dto.Code,
                Name = dto.Name,
                Channel = dto.Channel,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Budget = dto.Budget,
                CurrencyCode = dto.CurrencyCode,
                TargetLeadCount = dto.TargetLeadCount,
                CustomerGroupId = dto.CustomerGroupId,
                OwnerHumanId = dto.OwnerHumanId,
                BranchId = dto.BranchId,
                Description = dto.Description,
                StartImmediately = dto.StartImmediately
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
