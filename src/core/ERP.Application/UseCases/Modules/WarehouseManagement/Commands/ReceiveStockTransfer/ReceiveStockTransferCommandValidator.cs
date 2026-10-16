using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ReceiveStockTransfer
{
    public class ReceiveStockTransferCommandValidator : AbstractValidator<ReceiveStockTransferCommand>
    {
        public ReceiveStockTransferCommandValidator()
        {
            RuleFor(r => r.TransferId).NotEmpty().WithMessage("Which transfer?");
            RuleFor(r => r.ReceiveDate).NotEqual(default(DateOnly)).WithMessage("When did it arrive?");

            RuleForEach(r => r.Lines).SetValidator(new ReceiveLineValidator());

            RuleFor(r => r.Lines)
                .Must(lines => lines.Select(l => l.TransferItemId).Distinct().Count() == lines.Count)
                .WithMessage("The same line is received twice.");
        }
    }

    public class ReceiveLineValidator : AbstractValidator<StockTransferMovementLineDto>
    {
        public ReceiveLineValidator()
        {
            RuleFor(l => l.TransferItemId).NotEmpty().WithMessage("Every line names a transfer line.");

            // Zero is allowed here, unlike on dispatch: a pallet that left and never arrived is a
            // real thing that has to be recordable. The handler insists on a reason for it.
            RuleFor(l => l.Quantity).GreaterThanOrEqualTo(0m).WithMessage("A quantity cannot be negative.");

            RuleFor(l => l.DiscrepancyReason)
                .MaximumLength(500);
        }
    }
}
