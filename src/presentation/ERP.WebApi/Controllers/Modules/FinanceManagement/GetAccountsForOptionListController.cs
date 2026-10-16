using ERP.Application.Common.ResponseDTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetAccountsForOptionList;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Account/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AccountSwaggerGroup)]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "GetAccountsForOptionList")]
    public class GetAccountsForOptionListController : BaseController<IReadOnlyList<GetResponseForOptionListDto>>
    {
        public GetAccountsForOptionListController(IMediator mediator, Serilog.ILogger logger) : base(mediator, logger)
        {
        }

        [HttpGet(Name = "GetAccountsForOptionList")]
        public async Task<IActionResult> Index([FromQuery] string? search)
        {
            var query = new GetAccountsForOptionListQuery { Search = search };
            return FromResult(await _mediator.Send(query));
        }
    }
}
