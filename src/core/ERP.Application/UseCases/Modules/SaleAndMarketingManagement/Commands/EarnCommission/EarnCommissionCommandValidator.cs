namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.EarnCommission
{
    public class EarnCommissionCommandValidator : AbstractValidator<EarnCommissionCommand>
    {
        public EarnCommissionCommandValidator()
        {
            RuleFor(r => r.SalesRepHumanId).NotEmpty().WithMessage("Who earned it?");

            // The reference is what stops the same sale paying commission twice.
            RuleFor(r => r.ReferenceType)
                .NotEmpty().WithMessage("What was it earned on?")
                .MaximumLength(100);

            RuleFor(r => r.ReferenceId).NotEmpty().WithMessage("Which one?");

            RuleFor(r => r.EarnedOn).NotEqual(default(DateOnly)).WithMessage("When was it earned?");

            RuleFor(r => r.BaseAmount).GreaterThan(0m).WithMessage("Commission on nothing is nothing.");

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            // A hundred percent commission is not a typo anybody should be able to make quietly.
            RuleFor(r => r.Rate)
                .InclusiveBetween(0m, 100m).WithMessage("A rate runs from zero to a hundred.")
                .LessThanOrEqualTo(50m).WithMessage("A rate above fifty percent needs to be set up as a different arrangement.");

            RuleFor(r => r.Description).MaximumLength(500);
        }
    }
}
