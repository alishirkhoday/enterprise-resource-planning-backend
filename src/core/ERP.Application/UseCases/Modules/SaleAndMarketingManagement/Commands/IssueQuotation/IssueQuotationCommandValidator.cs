using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.IssueQuotation
{
    public class IssueQuotationCommandValidator : AbstractValidator<IssueQuotationCommand>
    {
        public IssueQuotationCommandValidator()
        {
            RuleFor(r => r.CustomerId).NotEmpty().WithMessage("Who is being quoted?");

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.Lines).NotEmpty().WithMessage("A quotation with no lines offers nothing.");

            // A quotation that never lapses is a price the customer can hold you to forever.
            RuleFor(r => r.ValidForDays)
                .InclusiveBetween(1, 365)
                .WithMessage("A quotation holds for between one day and a year.");

            RuleFor(r => r.AdditionalCharges).GreaterThanOrEqualTo(0m).WithMessage("A charge cannot be negative.");
            RuleFor(r => r.HeaderDiscountPercentage).InclusiveBetween(0m, 100m);
            RuleFor(r => r.Terms).MaximumLength(4000);

            RuleForEach(r => r.Lines).SetValidator(new QuotationLineValidator());
        }
    }

    public class QuotationLineValidator : AbstractValidator<QuotationLineDto>
    {
        public QuotationLineValidator()
        {
            RuleFor(l => l.ItemId).NotEmpty().WithMessage("Every line names an item.");
            RuleFor(l => l.UnitOfMeasureId).NotEmpty().WithMessage("Every line states its unit.");
            RuleFor(l => l.Quantity).GreaterThan(0m).WithMessage("A line of nothing is not a line.");

            RuleFor(l => l.UnitPrice)
                .GreaterThanOrEqualTo(0m).WithMessage("A price cannot be negative.")
                .When(l => l.UnitPrice is not null);

            RuleFor(l => l.DiscountPercentage)
                .InclusiveBetween(0m, 100m)
                .When(l => l.DiscountPercentage is not null);

            RuleFor(l => l.TaxPercentage)
                .InclusiveBetween(0m, 100m)
                .When(l => l.TaxPercentage is not null);

            RuleFor(l => l.TaxId)
                .NotEmpty().WithMessage("A tax rate needs the tax it belongs to.")
                .When(l => l.TaxPercentage is > 0m);

            RuleFor(l => l.DeliveryLeadTimeDays)
                .GreaterThan(0).WithMessage("A lead time of nothing is not a lead time.")
                .When(l => l.DeliveryLeadTimeDays is not null);
        }
    }
}
