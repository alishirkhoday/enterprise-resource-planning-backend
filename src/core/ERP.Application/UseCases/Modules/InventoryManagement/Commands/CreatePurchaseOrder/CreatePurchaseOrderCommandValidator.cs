using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreatePurchaseOrder
{
    public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
    {
        public CreatePurchaseOrderCommandValidator()
        {
            RuleFor(o => o.SupplierId).NotEmpty().WithMessage("An order needs a supplier.");

            RuleFor(o => o.CurrencyCode)
                .NotEmpty().WithMessage("A currency is required.")
                .Matches("^[A-Za-z]{3}$").WithMessage("A currency code is three letters.");

            RuleFor(o => o.Lines).NotEmpty().WithMessage("An order needs at least one line.");

            RuleForEach(o => o.Lines).SetValidator(new PurchaseOrderLineValidator());

            RuleFor(o => o.AdditionalCosts)
                .GreaterThanOrEqualTo(0m)
                .WithMessage("Additional costs cannot be negative.");
        }
    }

    public class PurchaseOrderLineValidator : AbstractValidator<PurchaseOrderLineDto>
    {
        public PurchaseOrderLineValidator()
        {
            RuleFor(l => l.ItemId).NotEmpty().WithMessage("Every line needs an item.");
            RuleFor(l => l.UnitOfMeasureId).NotEmpty().WithMessage("Every line needs a unit of measure.");
            RuleFor(l => l.Quantity).GreaterThan(0m).WithMessage("Every line needs a quantity above zero.");

            RuleFor(l => l.UnitPrice)
                .GreaterThanOrEqualTo(0m)
                .WithMessage("A price cannot be negative.")
                .When(l => l.UnitPrice is not null);

            RuleFor(l => l.DiscountPercentage)
                .InclusiveBetween(0m, 100m)
                .WithMessage("A discount is a percentage.")
                .When(l => l.DiscountPercentage is not null);

            RuleFor(l => l.TaxPercentage)
                .InclusiveBetween(0m, 100m)
                .WithMessage("A tax rate is a percentage.")
                .When(l => l.TaxPercentage is not null);
        }
    }
}
