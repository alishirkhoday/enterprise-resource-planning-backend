namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApprovePurchaseOrder
{
    public class ApprovePurchaseOrderCommandValidator : AbstractValidator<ApprovePurchaseOrderCommand>
    {
        public ApprovePurchaseOrderCommandValidator()
        {
            RuleFor(o => o.PurchaseOrderId).NotEmpty().WithMessage("Which order?");

            // Approving needs no explanation; refusing somebody's order does.
            RuleFor(o => o.Reason)
                .NotEmpty()
                .WithMessage("A rejected order needs a reason.")
                .When(o => !o.IsApproved);
        }
    }
}
