using ERP.Application.UseCases.Modules.FinanceManagement.Commands.RunDepreciation;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/FixedAsset/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.FixedAssetSwaggerGroup)]
    public class RunDepreciationController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<DepreciationRunDto>(mediator, logger)
    {
        /// <summary>
        /// Charge depreciation for a period
        /// </summary>
        /// <remarks>
        /// A period close activity: everything in service is charged together, because a month
        /// with one machine missed is wrong for the whole company. Assets the run passes over are
        /// reported with a reason rather than dropped. PreviewOnly works out the charges and
        /// writes nothing, which is what a controller does before committing to a month end.
        /// </remarks>
        [HttpPost(Name = "RunDepreciation")]
        public async Task<IActionResult> Index([FromBody] RunDepreciationDto dto)
        {
            var command = new RunDepreciationCommand
            {
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                CategoryId = dto.CategoryId,
                BranchId = dto.BranchId,
                DocumentId = dto.DocumentId,
                AccountingPeriodId = dto.AccountingPeriodId,
                Production = dto.Production,
                PreviewOnly = dto.PreviewOnly
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
