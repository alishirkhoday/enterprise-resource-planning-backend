using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.EarnCommission;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Commission/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.CommissionSwaggerGroup)]
    public class EarnCommissionController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<CommissionDto>(mediator, logger)
    {
        /// <summary>
        /// Record commission earned on a sale
        /// </summary>
        /// <remarks>
        /// Accrued, not owed: commission on an invoice the customer has not paid is a liability
        /// against money that may never arrive. The document reference is checked so one sale
        /// cannot pay commission twice.
        /// </remarks>
        [HttpPost(Name = "EarnCommission")]
        public async Task<IActionResult> Index([FromBody] EarnCommissionDto dto)
        {
            var command = new EarnCommissionCommand
            {
                SalesRepHumanId = dto.SalesRepHumanId,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                EarnedOn = dto.EarnedOn,
                BaseAmount = dto.BaseAmount,
                CurrencyCode = dto.CurrencyCode,
                Rate = dto.Rate,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
