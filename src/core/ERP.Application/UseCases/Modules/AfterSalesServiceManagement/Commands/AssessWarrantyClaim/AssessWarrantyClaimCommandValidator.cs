using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.AssessWarrantyClaim
{
    public class AssessWarrantyClaimCommandValidator : AbstractValidator<AssessWarrantyClaimCommand>
    {
        public AssessWarrantyClaimCommandValidator()
        {
            RuleFor(c => c.ClaimId).NotEmpty().WithMessage("Which claim?");
            RuleFor(c => c.AssessedOn).NotEqual(default(DateOnly)).WithMessage("When was it assessed?");

            RuleFor(c => c.Resolution)
                .NotEqual(WarrantyClaimResolution.Pending)
                .WithMessage("An accepted claim has to say how it will be honoured.")
                .When(c => c.IsApproved);

            RuleFor(c => c.RejectionReason)
                .NotEmpty()
                .WithMessage("A refused claim needs a reason the customer can be told.")
                .When(c => !c.IsApproved);
        }
    }
}
