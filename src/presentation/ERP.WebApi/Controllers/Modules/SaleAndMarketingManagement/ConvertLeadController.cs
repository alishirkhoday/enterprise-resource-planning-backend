using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ConvertLead;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Lead/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.LeadSwaggerGroup)]
    public class ConvertLeadController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ConvertLeadResultDto>(mediator, logger)
    {
        /// <summary>
        /// Turn a qualified lead into a customer
        /// </summary>
        /// <remarks>
        /// The customer is created here rather than by hand, which is what keeps the chain from
        /// campaign to customer intact. A deal can be opened in the same step, and it inherits
        /// the lead's campaign so marketing spend stays answerable against revenue.
        /// </remarks>
        [HttpPost(Name = "ConvertLead")]
        public async Task<IActionResult> Index([FromBody] ConvertLeadDto dto)
        {
            var command = new ConvertLeadCommand
            {
                LeadId = dto.LeadId,
                ConvertedOn = dto.ConvertedOn,
                ExistingCustomerId = dto.ExistingCustomerId,
                CustomerType = dto.CustomerType,
                Opportunity = dto.Opportunity
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
