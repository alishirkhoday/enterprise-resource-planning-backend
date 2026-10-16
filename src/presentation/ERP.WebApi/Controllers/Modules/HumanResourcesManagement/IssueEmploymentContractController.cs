using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.IssueEmploymentContract;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.HumanResourcesManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Human/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.HumanSwaggerGroup)]
    public class IssueEmploymentContractController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<EmploymentContractDto>(mediator, logger)
    {
        /// <summary>
        /// Issue an employment contract
        /// </summary>
        /// <remarks>
        /// Two contracts cannot be in effect on the same day. Payroll asks for the one effective
        /// on a date and takes a single answer, so an overlap decides somebody's salary by
        /// whichever row comes back first.
        /// </remarks>
        [HttpPost(Name = "IssueEmploymentContract")]
        public async Task<IActionResult> Index([FromBody] IssueEmploymentContractDto dto)
        {
            var command = new IssueEmploymentContractCommand
            {
                HumanId = dto.HumanId,
                PositionId = dto.PositionId,
                EmploymentType = dto.EmploymentType,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                BaseSalary = dto.BaseSalary,
                CurrencyCode = dto.CurrencyCode,
                ContractNumber = dto.ContractNumber,
                WeeklyWorkingHours = dto.WeeklyWorkingHours,
                ProbationEndDate = dto.ProbationEndDate,
                CostCenterId = dto.CostCenterId,
                WorkCalendarId = dto.WorkCalendarId,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
