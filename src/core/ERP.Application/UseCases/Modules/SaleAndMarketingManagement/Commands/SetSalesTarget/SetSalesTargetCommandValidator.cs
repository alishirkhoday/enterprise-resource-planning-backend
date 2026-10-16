namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.SetSalesTarget
{
    public class SetSalesTargetCommandValidator : AbstractValidator<SetSalesTargetCommand>
    {
        public SetSalesTargetCommandValidator()
        {
            RuleFor(r => r.Title)
                .NotEmpty().WithMessage("A target needs a title.")
                .MaximumLength(200);

            RuleFor(r => r.Scope).IsInEnum();
            RuleFor(r => r.ScopeReferenceId).NotEmpty().WithMessage("Whose target is it?");

            RuleFor(r => r.StartDate).NotEqual(default(DateOnly)).WithMessage("When does the period start?");

            RuleFor(r => r.EndDate)
                .GreaterThanOrEqualTo(r => r.StartDate)
                .WithMessage("A period cannot end before it starts.")
                .When(r => r.EndDate is not null);

            RuleFor(r => r.TargetAmount).GreaterThan(0m).WithMessage("A target of nothing is not a target.");

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.TargetCount)
                .GreaterThan(0).WithMessage("A count target of nothing is not a target.")
                .When(r => r.TargetCount is not null);

            RuleFor(r => r.Description).MaximumLength(1000);
        }
    }
}
