using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ApproveCommission;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Commission/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.CommissionSwaggerGroup)]
    public class ApproveCommissionController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<CommissionDto>(mediator, logger)
    {
        /// <summary>
        /// Confirm commission is owed, or take it back
        /// </summary>
        /// <remarks>
        /// Approving is what moves it from a provision to a debt, which normally waits until the
        /// customer has paid. Reversing is how commission on a sale that was later credited gets
        /// undone before it reaches payroll; a paid one has to be clawed back through payroll.
        /// </remarks>
        [HttpPost(Name = "ApproveCommission")]
        public async Task<IActionResult> Index([FromBody] ApproveCommissionDto dto)
        {
            var command = new ApproveCommissionCommand
            {
                CommissionId = dto.CommissionId,
                IsApproved = dto.IsApproved,
                ReversalReason = dto.ReversalReason
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
