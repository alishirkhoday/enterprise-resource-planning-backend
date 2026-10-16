using ERP.Application.UseCases.Modules.OrderManagement.DTOs;

namespace ERP.Application.UseCases.Modules.OrderManagement.Commands.PlaceOrder
{
    public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
    {
        /// <summary>
        /// Long enough for a bank transfer to clear, short enough that stock is not held all day
        /// by somebody who has changed their mind.
        /// </summary>
        private const int MinimumPaymentWindowMinutes = 5;
        private const int MaximumPaymentWindowMinutes = 1_440;

        public PlaceOrderCommandValidator()
        {
            RuleFor(o => o.CustomerId)
                .NotEmpty()
                .WithMessage("An order needs a customer.");

            RuleFor(o => o.CurrencyCode)
                .NotEmpty().WithMessage("A currency is required.")
                .Length(3).WithMessage("A currency code is three letters.")
                .Matches("^[A-Za-z]{3}$").WithMessage("A currency code is three letters.");

            RuleFor(o => o.Items)
                .NotEmpty()
                .WithMessage("An order needs at least one line.");

            RuleForEach(o => o.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ItemId).NotEmpty().WithMessage("Each line needs an item.");
                item.RuleFor(i => i.Quantity).GreaterThan(0m).WithMessage("Each line needs a quantity above zero.");
            });

            // The same item twice would take two price lookups and two volume breaks for what is
            // one quantity, so the caller is asked to combine them rather than guess which wins.
            RuleFor(o => o.Items)
                .Must(items => items.Select(i => i.ItemId).Distinct().Count() == items.Count)
                .WithMessage("The same item appears on more than one line; combine them.")
                .When(o => o.Items.Count > 0);

            RuleFor(o => o.PaymentWindowMinutes)
                .InclusiveBetween(MinimumPaymentWindowMinutes, MaximumPaymentWindowMinutes)
                .WithMessage($"The payment window has to be between {MinimumPaymentWindowMinutes} and {MaximumPaymentWindowMinutes} minutes.");

            RuleFor(o => o.ShipTo)
                .NotNull()
                .WithMessage("An order needs a delivery address.");

            When(o => o.ShipTo is not null, () =>
            {
                RuleFor(o => o.ShipTo).SetValidator(new PlaceOrderAddressValidator());
            });
        }
    }

    public class PlaceOrderAddressValidator : AbstractValidator<PlaceOrderAddressDto>
    {
        public PlaceOrderAddressValidator()
        {
            RuleFor(a => a.ReceiverFullName).NotEmpty().WithMessage("Somebody has to sign for it.");
            RuleFor(a => a.ReceiverMobileNumber).NotEmpty().WithMessage("A mobile number is needed for the delivery.");
            RuleFor(a => a.Country).NotEmpty().WithMessage("A country is required.");
            RuleFor(a => a.Province).NotEmpty().WithMessage("A province is required.");
            RuleFor(a => a.City).NotEmpty().WithMessage("A city is required.");
            RuleFor(a => a.Region).NotEmpty().WithMessage("A region is required.");
            RuleFor(a => a.Street).NotEmpty().WithMessage("A street is required.");
            RuleFor(a => a.Plaque).NotEmpty().WithMessage("A plaque number is required.");
            RuleFor(a => a.PostalCode).NotEmpty().WithMessage("A postal code is required.");
        }
    }
}
