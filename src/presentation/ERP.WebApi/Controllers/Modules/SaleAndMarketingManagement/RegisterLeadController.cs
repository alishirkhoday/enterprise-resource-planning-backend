using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.RegisterLead;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Lead/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.LeadSwaggerGroup)]
    public class RegisterLeadController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<LeadDto>(mediator, logger)
    {
        /// <summary>
        /// Record a new lead
        /// </summary>
        /// <remarks>
        /// The mobile number is checked against open leads: the same person filling in a form
        /// twice, or being entered by two salespeople from one trade show, is how a sales database
        /// rots. A lead that was converted or disqualified long ago does not block a new one.
        /// </remarks>
        [HttpPost(Name = "RegisterLead")]
        public async Task<IActionResult> Index([FromBody] RegisterLeadDto dto)
        {
            var command = new RegisterLeadCommand
            {
                FullName = dto.FullName,
                MobilePhoneNumberWithRegionCode = dto.MobilePhoneNumberWithRegionCode,
                Source = dto.Source,
                CompanyName = dto.CompanyName,
                JobTitle = dto.JobTitle,
                Email = dto.Email,
                CampaignId = dto.CampaignId,
                AssignedToHumanId = dto.AssignedToHumanId,
                BranchId = dto.BranchId,
                Note = dto.Note
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
