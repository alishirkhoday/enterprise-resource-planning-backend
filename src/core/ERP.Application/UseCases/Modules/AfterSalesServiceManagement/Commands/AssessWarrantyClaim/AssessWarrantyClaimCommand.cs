using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.AssessWarrantyClaim
{
    /// <summary>
    /// Decides a claim. An acceptance also says how it will be honoured, because the work that
    /// follows - a repair, a replacement, a credit - depends on which one it is.
    /// </summary>
    public sealed record AssessWarrantyClaimCommand : ICommand<Result<WarrantyClaimDto, Error>>
    {
        public Guid ClaimId { get; init; }

        public bool IsApproved { get; init; }

        public DateOnly AssessedOn { get; init; }

        public WarrantyClaimResolution Resolution { get; init; }

        public string? Findings { get; init; }

        public string? RejectionReason { get; init; }
    }
}
