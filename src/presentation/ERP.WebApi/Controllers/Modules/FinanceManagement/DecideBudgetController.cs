using ERP.Application.UseCases.Modules.FinanceManagement.Commands.DecideBudget;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Budget/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.BudgetSwaggerGroup)]
    public class DecideBudgetController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<BudgetDto>(mediator, logger)
    {
        /// <summary>
        /// Approve or reject a submitted budget
        /// </summary>
        /// <remarks>
        /// Approval is what turns a plan into a control: until it happens the budget check has
        /// nothing to measure a spend against. A rejection sends it back to draft so the numbers
        /// can be reworked rather than the whole budget being raised again.
        /// </remarks>
        [HttpPost(Name = "DecideBudget")]
        public async Task<IActionResult> Index([FromBody] DecideBudgetDto dto)
        {
            var command = new DecideBudgetCommand
            {
                BudgetId = dto.BudgetId,
                IsApproved = dto.IsApproved,
                ApprovedBy = dto.ApprovedBy
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
