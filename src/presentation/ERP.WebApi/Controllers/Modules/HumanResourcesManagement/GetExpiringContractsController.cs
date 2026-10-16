using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.Queries.GetExpiringContracts;

namespace ERP.WebApi.Controllers.Modules.HumanResourcesManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Human/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.HumanSwaggerGroup)]
    public class GetExpiringContractsController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<IReadOnlyList<ExpiringContractDto>>(mediator, logger)
    {
        /// <summary>
        /// Employment contracts about to lapse
        /// </summary>
        /// <remarks>
        /// Somebody working with no agreement behind them is the thing nobody spots until payroll
        /// fails, and by then the gap is weeks old. Open ended contracts never appear here, and
        /// neither do ones already terminated.
        /// </remarks>
        [HttpGet(Name = "GetExpiringContracts")]
        public async Task<IActionResult> Index([FromQuery] int withinDays = 30, [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null)
        {
            var query = new GetExpiringContractsQuery
            {
                WithinDays = withinDays,
                From = from,
                To = to
            };

            return FromResult(await _mediator.Send(query));
        }
    }
}
