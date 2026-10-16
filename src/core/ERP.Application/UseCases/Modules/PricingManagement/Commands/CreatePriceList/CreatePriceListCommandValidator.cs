using ERP.Application.UseCases.Modules.PricingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.PricingManagement.Commands.CreatePriceList
{
    public class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
    {
        public CreatePriceListCommandValidator()
        {
            RuleFor(r => r.Code)
                .NotEmpty().WithMessage("A price list needs a code.")
                .MaximumLength(50);

            RuleFor(r => r.Name)
                .NotEmpty().WithMessage("A price list needs a name.")
                .MaximumLength(200);

            RuleFor(r => r.Type).IsInEnum();

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.StartDate).NotEqual(default(DateOnly)).WithMessage("When does it come into force?");

            RuleFor(r => r.EndDate)
                .GreaterThanOrEqualTo(r => r.StartDate)
                .WithMessage("A price list cannot lapse before it starts.")
                .When(r => r.EndDate is not null);

            RuleFor(r => r.Description).MaximumLength(1000);

            RuleForEach(r => r.Lines).SetValidator(new PriceListLineValidator());

            // The domain refuses a second line for the same break too, but as an exception. This
            // names both the item and the quantity instead.
            RuleFor(r => r.Lines)
                .Must(lines => lines.Select(l => (l.ItemId, l.MinimumQuantity)).Distinct().Count() == lines.Count)
                .WithMessage("The same item is priced twice at the same quantity break.");
        }
    }

    public class PriceListLineValidator : AbstractValidator<PriceListLineDto>
    {
        public PriceListLineValidator()
        {
            RuleFor(l => l.ItemId).NotEmpty().WithMessage("Every line names an item.");
            RuleFor(l => l.UnitOfMeasureId).NotEmpty().WithMessage("Every line states its unit.");

            // Zero is allowed: a free item on a promotional list is a real thing.
            RuleFor(l => l.BasePrice).GreaterThanOrEqualTo(0m).WithMessage("A price cannot be negative.");

            RuleFor(l => l.MinimumQuantity).GreaterThan(0m).WithMessage("A quantity break starts above zero.");

            RuleFor(l => l.DiscountPercentage)
                .InclusiveBetween(0m, 100m)
                .When(l => l.DiscountPercentage is not null);

            RuleFor(l => l.TaxPercentage)
                .InclusiveBetween(0m, 100m)
                .When(l => l.TaxPercentage is not null);

            RuleFor(l => l.MinimumPrice)
                .GreaterThanOrEqualTo(0m).WithMessage("A floor cannot be negative.")
                .LessThanOrEqualTo(l => l.BasePrice).WithMessage("A floor above the base price would refuse every sale.")
                .When(l => l.MinimumPrice is not null);
        }
    }
}
