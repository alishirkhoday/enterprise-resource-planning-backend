using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetTrialBalance;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Account/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AccountSwaggerGroup)]
    public class GetTrialBalanceController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<TrialBalanceDto>(mediator, logger)
    {
        /// <summary>
        /// The trial balance for a window
        /// </summary>
        /// <remarks>
        /// Accounts that did not move are left out unless asked for, which is what keeps the
        /// report readable. If the debit and credit totals differ, the books do not balance.
        /// </remarks>
        [HttpGet(Name = "GetTrialBalance")]
        public async Task<IActionResult> Index(
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to,
            [FromQuery] Guid? branchId,
            [FromQuery] bool includeAccountsWithNoMovement = false)
        {
            var query = new GetTrialBalanceQuery
            {
                From = from,
                To = to,
                BranchId = branchId,
                IncludeAccountsWithNoMovement = includeAccountsWithNoMovement
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
