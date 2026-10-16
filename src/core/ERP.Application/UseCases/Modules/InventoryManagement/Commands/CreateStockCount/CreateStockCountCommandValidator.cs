using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreateStockCount
{
    public class CreateStockCountCommandValidator : AbstractValidator<CreateStockCountCommand>
    {
        public CreateStockCountCommandValidator()
        {
            RuleFor(r => r.WarehouseId).NotEmpty().WithMessage("Which warehouse is being counted?");
            RuleFor(r => r.CountDate).NotEqual(default(DateOnly)).WithMessage("A count needs a date.");
            RuleFor(r => r.Type).IsInEnum();

            RuleFor(r => r.ItemIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("The same item is on the sheet twice.");

            // A full count means the whole warehouse. Naming items and calling it full is one of
            // the two that is wrong, and the sheet would not match what the label claims.
            RuleFor(r => r.ItemIds)
                .Empty()
                .WithMessage("A full count covers the whole warehouse; name items only for a cycle or spot count.")
                .When(r => r.Type == StockCountType.Full);

            RuleFor(r => r.ItemIds)
                .NotEmpty()
                .WithMessage("A spot count has to say what is being checked.")
                .When(r => r.Type == StockCountType.Spot);
        }
    }
}
