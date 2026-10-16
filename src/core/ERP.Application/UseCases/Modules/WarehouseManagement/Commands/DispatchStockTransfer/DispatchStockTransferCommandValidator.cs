using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.DispatchStockTransfer
{
    public class DispatchStockTransferCommandValidator : AbstractValidator<DispatchStockTransferCommand>
    {
        public DispatchStockTransferCommandValidator()
        {
            RuleFor(r => r.TransferId).NotEmpty().WithMessage("Which transfer?");
            RuleFor(r => r.DispatchDate).NotEqual(default(DateOnly)).WithMessage("When did it leave?");

            // An empty list is meaningful - it dispatches the whole transfer - so only named
            // lines are checked.
            RuleForEach(r => r.Lines).SetValidator(new DispatchLineValidator());

            RuleFor(r => r.Lines)
                .Must(lines => lines.Select(l => l.TransferItemId).Distinct().Count() == lines.Count)
                .WithMessage("The same line is dispatched twice.");
        }
    }

    public class DispatchLineValidator : AbstractValidator<StockTransferMovementLineDto>
    {
        public DispatchLineValidator()
        {
            RuleFor(l => l.TransferItemId).NotEmpty().WithMessage("Every line names a transfer line.");

            // Nothing dispatched is not a short dispatch, it is a line that should not be on the
            // truck. Leaving it off the request says that more clearly than sending a zero.
            RuleFor(l => l.Quantity).GreaterThan(0m).WithMessage("Dispatching nothing is not a dispatch.");
        }
    }
}
