namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CloseOpportunity
{
    public class CloseOpportunityCommandValidator : AbstractValidator<CloseOpportunityCommand>
    {
        public CloseOpportunityCommandValidator()
        {
            RuleFor(r => r.OpportunityId).NotEmpty().WithMessage("Which deal?");
            RuleFor(r => r.ClosedOn).NotEqual(default(DateOnly)).WithMessage("When did it close?");

            // The only free lesson a sales system produces. Losing without a reason throws it away.
            RuleFor(r => r.LostReason)
                .NotEmpty().WithMessage("Why was it lost?")
                .MaximumLength(1000)
                .When(r => !r.IsWon);
        }
    }
}
