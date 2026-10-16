namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.LaunchCampaign
{
    public class LaunchCampaignCommandValidator : AbstractValidator<LaunchCampaignCommand>
    {
        public LaunchCampaignCommandValidator()
        {
            RuleFor(r => r.Code)
                .NotEmpty().WithMessage("A campaign needs a code.")
                .MaximumLength(50);

            RuleFor(r => r.Name)
                .NotEmpty().WithMessage("A campaign needs a name.")
                .MaximumLength(200);

            RuleFor(r => r.Channel)
                .NotEmpty().WithMessage("How does it reach people?")
                .MaximumLength(100);

            RuleFor(r => r.StartDate).NotEqual(default(DateOnly)).WithMessage("When does it start?");

            RuleFor(r => r.EndDate)
                .GreaterThanOrEqualTo(r => r.StartDate)
                .WithMessage("A campaign cannot end before it starts.")
                .When(r => r.EndDate is not null);

            // A campaign with no budget cannot be answered for afterwards, which is the whole
            // reason it is a record rather than a spreadsheet.
            RuleFor(r => r.Budget).GreaterThan(0m).WithMessage("What is being spent on it?");

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.TargetLeadCount)
                .GreaterThan(0).WithMessage("A target of nothing is not a target.")
                .When(r => r.TargetLeadCount is not null);

            RuleFor(r => r.Description).MaximumLength(2000);
        }
    }
}
