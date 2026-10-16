using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.IssueInvoice
{
    public class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
    {
        public IssueInvoiceCommandValidator()
        {
            RuleFor(r => r.CustomerId).NotEmpty().WithMessage("Who is being invoiced?");
            RuleFor(r => r.IssueDate).NotEqual(default(DateOnly)).WithMessage("An invoice needs a date.");
            RuleFor(r => r.DueDate).NotEqual(default(DateOnly)).WithMessage("An invoice needs a due date.");

            // The domain refuses this too, but as an exception. Refusing it here returns the
            // caller a field and a sentence instead of a five hundred.
            RuleFor(r => r.DueDate)
                .GreaterThanOrEqualTo(r => r.IssueDate)
                .WithMessage("Payment cannot be due before the invoice exists.");

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.Lines).NotEmpty().WithMessage("An invoice with no lines bills nothing.");
            RuleFor(r => r.AdditionalCharges).GreaterThanOrEqualTo(0m).WithMessage("A charge cannot be negative.");
            RuleFor(r => r.HeaderDiscountPercentage).InclusiveBetween(0m, 100m);

            RuleForEach(r => r.Lines).SetValidator(new InvoiceLineValidator());
        }
    }

    public class InvoiceLineValidator : AbstractValidator<InvoiceLineDto>
    {
        public InvoiceLineValidator()
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

            // A tax rate with nothing to attribute it to cannot be reported on a return.
            RuleFor(l => l.TaxId)
                .NotEmpty().WithMessage("A tax rate needs the tax it belongs to.")
                .When(l => l.TaxPercentage is > 0m);
        }
    }
}
