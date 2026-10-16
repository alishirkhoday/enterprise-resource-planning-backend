using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.ReceiveGoods
{
    public class ReceiveGoodsCommandValidator : AbstractValidator<ReceiveGoodsCommand>
    {
        public ReceiveGoodsCommandValidator()
        {
            RuleFor(r => r.PurchaseOrderId).NotEmpty().WithMessage("Which order is this against?");
            RuleFor(r => r.WarehouseId).NotEmpty().WithMessage("Where did it go?");
            RuleFor(r => r.ReceiptDate).NotEqual(default(DateOnly)).WithMessage("When did it arrive?");
            RuleFor(r => r.Lines).NotEmpty().WithMessage("A receipt with no lines records nothing.");

            RuleForEach(r => r.Lines).SetValidator(new ReceiveGoodsLineValidator());
        }
    }

    public class ReceiveGoodsLineValidator : AbstractValidator<ReceiveGoodsLineDto>
    {
        public ReceiveGoodsLineValidator()
        {
            RuleFor(l => l.PurchaseOrderItemId).NotEmpty().WithMessage("Every line names an order line.");
            RuleFor(l => l.ReceivedQuantity).GreaterThan(0m).WithMessage("A line that received nothing is not a line.");
            RuleFor(l => l.RejectedQuantity).GreaterThanOrEqualTo(0m);

            // More rejected than arrived is arithmetic nobody can act on.
            RuleFor(l => l)
                .Must(l => l.RejectedQuantity <= l.ReceivedQuantity)
                .WithMessage("More was rejected than arrived.");

            RuleFor(l => l.RejectionReason)
                .NotEmpty()
                .WithMessage("A rejection needs a reason.")
                .When(l => l.RejectedQuantity > 0m);

            RuleFor(l => l.UnitCost)
                .GreaterThanOrEqualTo(0m)
                .WithMessage("A cost cannot be negative.")
                .When(l => l.UnitCost is not null);
        }
    }
}
