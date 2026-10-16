using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.AssessWarrantyClaim;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

namespace ERP.WebApi.Controllers.Modules.AfterSalesServiceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/WarrantyClaim/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AfterSalesSwaggerGroup)]
    public class AssessWarrantyClaimController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<WarrantyClaimDto>(mediator, logger)
    {
        /// <summary>
        /// Decide a warranty claim
        /// </summary>
        /// <remarks>
        /// An acceptance also says how it will be honoured - repair, replace, refund or partial
        /// credit - because the work that follows depends on which one it is.
        /// </remarks>
        [HttpPost("{claimId:guid}", Name = "AssessWarrantyClaim")]
        public async Task<IActionResult> Index(Guid claimId, [FromBody] AssessWarrantyClaimRequest body)
        {
            var command = new AssessWarrantyClaimCommand
            {
                ClaimId = claimId,
                IsApproved = body.IsApproved,
                AssessedOn = body.AssessedOn,
                Resolution = body.Resolution,
                Findings = body.Findings,
                RejectionReason = body.RejectionReason
            };

            return FromResult(await _mediator.Send(command));
        }

        public sealed record AssessWarrantyClaimRequest(
            bool IsApproved,
            DateOnly AssessedOn,
            WarrantyClaimResolution Resolution,
            string? Findings,
            string? RejectionReason);
    }
}
