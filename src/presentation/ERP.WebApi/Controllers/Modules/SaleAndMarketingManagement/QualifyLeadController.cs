using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.QualifyLead;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Lead/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.LeadSwaggerGroup)]
    public class QualifyLeadController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<LeadDto>(mediator, logger)
    {
        /// <summary>
        /// Decide whether a lead is worth pursuing
        /// </summary>
        /// <remarks>
        /// One endpoint for both answers, because it is one decision taken at one moment.
        /// Disqualifying needs a reason: without it the same bad lead gets bought again.
        /// </remarks>
        [HttpPost(Name = "QualifyLead")]
        public async Task<IActionResult> Index([FromBody] QualifyLeadDto dto)
        {
            var command = new QualifyLeadCommand
            {
                LeadId = dto.LeadId,
                IsQualified = dto.IsQualified,
                Reason = dto.Reason,
                Score = dto.Score
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
