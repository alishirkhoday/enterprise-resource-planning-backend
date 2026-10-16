namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.AdvanceOpportunity
{
    public class AdvanceOpportunityCommandValidator : AbstractValidator<AdvanceOpportunityCommand>
    {
        public AdvanceOpportunityCommandValidator()
        {
            RuleFor(r => r.OpportunityId).NotEmpty().WithMessage("Which deal?");
            RuleFor(r => r.Stage).IsInEnum();

            RuleFor(r => r.Probability)
                .InclusiveBetween(0m, 100m).WithMessage("A probability runs from zero to a hundred.")
                .When(r => r.Probability is not null);

            RuleFor(r => r.EstimatedValue)
                .GreaterThan(0m).WithMessage("A deal worth nothing is not a deal.")
                .When(r => r.EstimatedValue is not null);

            RuleFor(r => r.CompetitorName).MaximumLength(200);
        }
    }
}
