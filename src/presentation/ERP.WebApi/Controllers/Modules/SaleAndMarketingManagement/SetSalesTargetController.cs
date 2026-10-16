using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.SetSalesTarget;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/SalesTarget/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.SalesTargetSwaggerGroup)]
    public class SetSalesTargetController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<SalesTargetDto>(mediator, logger)
    {
        /// <summary>
        /// Set a quota for a person, team, territory or branch
        /// </summary>
        /// <remarks>
        /// Overlapping targets are allowed: a quarterly target sits inside an annual one and both
        /// are real. A won deal credits every target whose period covers its closing date.
        /// </remarks>
        [HttpPost(Name = "SetSalesTarget")]
        public async Task<IActionResult> Index([FromBody] SetSalesTargetDto dto)
        {
            var command = new SetSalesTargetCommand
            {
                Title = dto.Title,
                Scope = dto.Scope,
                ScopeReferenceId = dto.ScopeReferenceId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TargetAmount = dto.TargetAmount,
                CurrencyCode = dto.CurrencyCode,
                TargetCount = dto.TargetCount,
                ItemGroupId = dto.ItemGroupId,
                BranchId = dto.BranchId,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
