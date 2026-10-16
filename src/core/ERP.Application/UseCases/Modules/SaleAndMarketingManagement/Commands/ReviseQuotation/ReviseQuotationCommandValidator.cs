using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.IssueQuotation;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ReviseQuotation
{
    public class ReviseQuotationCommandValidator : AbstractValidator<ReviseQuotationCommand>
    {
        public ReviseQuotationCommandValidator()
        {
            RuleFor(r => r.QuotationId).NotEmpty().WithMessage("Which quotation?");

            RuleFor(r => r.ValidForDays)
                .InclusiveBetween(1, 365).WithMessage("A quotation holds for between one day and a year.")
                .When(r => r.ValidForDays is not null);

            RuleFor(r => r.HeaderDiscountPercentage)
                .InclusiveBetween(0m, 100m)
                .When(r => r.HeaderDiscountPercentage is not null);

            RuleFor(r => r.Reason).MaximumLength(1000);

            // An empty list is meaningful: it carries the previous lines across unchanged.
            RuleForEach(r => r.Lines).SetValidator(new QuotationLineValidator());
        }
    }
}
