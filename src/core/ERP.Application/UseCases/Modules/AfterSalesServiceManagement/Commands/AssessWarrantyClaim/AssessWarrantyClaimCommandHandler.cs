using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Mapping;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.AssessWarrantyClaim
{
    /// <summary>
    /// Records the decision on a claim. Whether a claim in this state can still be decided is a
    /// rule the claim keeps for itself, so a second assessment is refused rather than quietly
    /// overwriting the first.
    /// </summary>
    public class AssessWarrantyClaimCommandHandler(IWarrantyClaimRepository claims)
        : IRequestHandler<AssessWarrantyClaimCommand, Result<WarrantyClaimDto, Error>>
    {
        public async Task<Result<WarrantyClaimDto, Error>> Handle(AssessWarrantyClaimCommand request, CancellationToken cancellationToken)
        {
            var claim = await claims.GetByIdAsync(request.ClaimId, cancellationToken);
            if (claim is null)
                return Result.Failure<WarrantyClaimDto, Error>(Errors.General.NotFound(request.ClaimId.ToString(), nameof(WarrantyClaim)));

            try
            {
                if (request.IsApproved)
                    claim.Approve(request.AssessedOn, request.Resolution, request.Findings);
                else
                    claim.Reject(request.AssessedOn, request.RejectionReason!, request.Findings);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<WarrantyClaimDto, Error>(Errors.AfterSales.ClaimCannotBeDecided(exception.Message));
            }

            await claims.UpdateAsync(claim, cancellationToken);

            return Result.Success<WarrantyClaimDto, Error>(claim.ToDto());
        }
    }
}
