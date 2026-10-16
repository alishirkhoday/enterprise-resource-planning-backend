namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RaiseServiceOrder
{
    public class RaiseServiceOrderCommandValidator : AbstractValidator<RaiseServiceOrderCommand>
    {
        public RaiseServiceOrderCommandValidator()
        {
            RuleFor(s => s.CustomerId).NotEmpty().WithMessage("Whose equipment?");
            RuleFor(s => s.ReportedFault).NotEmpty().WithMessage("What is wrong with it?");
            RuleFor(s => s.Priority).IsInEnum();

            RuleFor(s => s.CurrencyCode)
                .NotEmpty().WithMessage("A currency is required.")
                .Matches("^[A-Za-z]{3}$").WithMessage("A currency code is three letters.");

            // A serial number identifies a unit of some item, so naming one without the other
            // leaves cover impossible to work out.
            RuleFor(s => s.ItemId)
                .NotNull()
                .WithMessage("A serial number needs the item it belongs to.")
                .When(s => s.SerialNumberId is not null);
        }
    }
}
