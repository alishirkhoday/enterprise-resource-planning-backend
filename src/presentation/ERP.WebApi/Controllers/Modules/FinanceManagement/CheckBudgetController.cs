using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Queries.CheckBudget;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Budget/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.BudgetSwaggerGroup)]
    public class CheckBudgetController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<BudgetCheckDto>(mediator, logger)
    {
        /// <summary>
        /// Ask whether a spend still fits inside the budget
        /// </summary>
        /// <remarks>
        /// Read only: it answers, it does not reserve, so a price check never quietly eats
        /// budget. Available is planned less actual less committed, so money already promised to
        /// a purchase order counts before the invoice arrives.
        /// </remarks>
        [HttpGet(Name = "CheckBudget")]
        public async Task<IActionResult> Index(
            [FromQuery] Guid financialYearId,
            [FromQuery] Guid accountId,
            [FromQuery] short periodNumber,
            [FromQuery] decimal amount,
            [FromQuery] Guid? costCenterId)
        {
            var query = new CheckBudgetQuery
            {
                FinancialYearId = financialYearId,
                AccountId = accountId,
                PeriodNumber = periodNumber,
                Amount = amount,
                CostCenterId = costCenterId
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
