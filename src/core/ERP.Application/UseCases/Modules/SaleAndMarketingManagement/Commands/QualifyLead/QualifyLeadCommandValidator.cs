namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.QualifyLead
{
    public class QualifyLeadCommandValidator : AbstractValidator<QualifyLeadCommand>
    {
        public QualifyLeadCommandValidator()
        {
            RuleFor(r => r.LeadId).NotEmpty().WithMessage("Which lead?");

            // Saying no without saying why is what makes the same bad lead get bought again.
            RuleFor(r => r.Reason)
                .NotEmpty().WithMessage("Why is this lead not worth pursuing?")
                .MaximumLength(500)
                .When(r => !r.IsQualified);

            RuleFor(r => r.Score)
                .InclusiveBetween(0, 100).WithMessage("A score runs from zero to a hundred.")
                .When(r => r.Score is not null);
        }
    }
}
