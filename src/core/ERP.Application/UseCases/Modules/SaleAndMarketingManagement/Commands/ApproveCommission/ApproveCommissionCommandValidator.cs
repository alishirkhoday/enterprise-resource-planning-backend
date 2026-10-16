namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ApproveCommission
{
    public class ApproveCommissionCommandValidator : AbstractValidator<ApproveCommissionCommand>
    {
        public ApproveCommissionCommandValidator()
        {
            RuleFor(r => r.CommissionId).NotEmpty().WithMessage("Which commission?");

            RuleFor(r => r.ReversalReason)
                .NotEmpty().WithMessage("Why is it being taken back?")
                .MaximumLength(500)
                .When(r => !r.IsApproved);
        }
    }
}
