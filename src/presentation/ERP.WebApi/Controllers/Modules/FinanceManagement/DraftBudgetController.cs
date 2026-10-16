using ERP.Application.UseCases.Modules.FinanceManagement.Commands.DraftBudget;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Budget/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.BudgetSwaggerGroup)]
    public class DraftBudgetController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<BudgetDto>(mediator, logger)
    {
        /// <summary>
        /// Draw up a budget for a financial year
        /// </summary>
        /// <remarks>
        /// Lines are per account per period, which is the grain at which spending is actually
        /// controlled: an annual figure tells nobody in March whether they are ahead or behind.
        /// Only final accounts can be budgeted, because a group account never carries a posting.
        /// </remarks>
        [HttpPost(Name = "DraftBudget")]
        public async Task<IActionResult> Index([FromBody] DraftBudgetDto dto)
        {
            var command = new DraftBudgetCommand
            {
                FinancialYearId = dto.FinancialYearId,
                Title = dto.Title,
                Type = dto.Type,
                CurrencyCode = dto.CurrencyCode,
                CostCenterId = dto.CostCenterId,
                BranchId = dto.BranchId,
                ProjectId = dto.ProjectId,
                Lines = dto.Lines,
                Description = dto.Description,
                SubmitImmediately = dto.SubmitImmediately
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
