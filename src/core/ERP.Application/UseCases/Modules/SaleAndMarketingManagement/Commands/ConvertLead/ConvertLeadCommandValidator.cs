using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ConvertLead
{
    public class ConvertLeadCommandValidator : AbstractValidator<ConvertLeadCommand>
    {
        public ConvertLeadCommandValidator()
        {
            RuleFor(r => r.LeadId).NotEmpty().WithMessage("Which lead?");
            RuleFor(r => r.ConvertedOn).NotEqual(default(DateOnly)).WithMessage("When did they become a customer?");
            RuleFor(r => r.CustomerType).IsInEnum();

            RuleFor(r => r.Opportunity!)
                .SetValidator(new OpenOpportunityValidator())
                .When(r => r.Opportunity is not null);
        }
    }

    public class OpenOpportunityValidator : AbstractValidator<OpenOpportunityDto>
    {
        public OpenOpportunityValidator()
        {
            RuleFor(o => o.Title)
                .NotEmpty().WithMessage("A deal needs a title.")
                .MaximumLength(200);

            RuleFor(o => o.EstimatedValue).GreaterThan(0m).WithMessage("A deal worth nothing is not a deal.");

            RuleFor(o => o.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(o => o.ExpectedCloseDate).NotEqual(default(DateOnly)).WithMessage("When is it expected to close?");
        }
    }
}
