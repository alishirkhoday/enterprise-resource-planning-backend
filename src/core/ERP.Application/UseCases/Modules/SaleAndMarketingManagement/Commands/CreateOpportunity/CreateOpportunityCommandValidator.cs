namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CreateOpportunity
{
    public class CreateOpportunityCommandValidator : AbstractValidator<CreateOpportunityCommand>
    {
        public CreateOpportunityCommandValidator()
        {
            RuleFor(r => r.Title)
                .NotEmpty().WithMessage("A deal needs a title.")
                .MaximumLength(200);

            RuleFor(r => r.EstimatedValue).GreaterThan(0m).WithMessage("A deal worth nothing is not a deal.");

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.ExpectedCloseDate).NotEqual(default(DateOnly)).WithMessage("When is it expected to close?");

            // A deal belonging to nobody cannot be chased and still counts toward the forecast.
            RuleFor(r => r)
                .Must(r => r.CustomerId is not null || r.SourceLeadId is not null)
                .WithMessage("A deal has to belong to either a customer or a lead.");

            RuleFor(r => r.CompetitorName).MaximumLength(200);
            RuleFor(r => r.Description).MaximumLength(2000);
        }
    }
}
