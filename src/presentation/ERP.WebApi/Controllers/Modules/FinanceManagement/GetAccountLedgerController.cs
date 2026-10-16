using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetAccountLedger;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Account/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AccountSwaggerGroup)]
    public class GetAccountLedgerController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<AccountLedgerDto>(mediator, logger)
    {
        /// <summary>
        /// One account's posted movements over a window
        /// </summary>
        /// <remarks>
        /// Drafts are excluded. The opening balance comes from everything posted before the window
        /// and the running balance is signed by the account's own normal balance, so a debit on a
        /// liability reduces it.
        /// </remarks>
        [HttpGet("{accountId:guid}", Name = "GetAccountLedger")]
        public async Task<IActionResult> Index(
            Guid accountId,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to,
            [FromQuery] Guid? detailAccountId,
            [FromQuery] Guid? branchId)
        {
            var query = new GetAccountLedgerQuery
            {
                AccountId = accountId,
                From = from,
                To = to,
                DetailAccountId = detailAccountId,
                BranchId = branchId
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
