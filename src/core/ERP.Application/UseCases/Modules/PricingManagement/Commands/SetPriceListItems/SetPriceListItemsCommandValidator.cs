using ERP.Application.UseCases.Modules.PricingManagement.Commands.CreatePriceList;

namespace ERP.Application.UseCases.Modules.PricingManagement.Commands.SetPriceListItems
{
    public class SetPriceListItemsCommandValidator : AbstractValidator<SetPriceListItemsCommand>
    {
        public SetPriceListItemsCommandValidator()
        {
            RuleFor(r => r.PriceListId).NotEmpty().WithMessage("Which price list?");

            // Replacing everything with nothing would leave a live list pricing nothing at all.
            RuleFor(r => r.Lines).NotEmpty().WithMessage("A request with no lines changes no prices.");

            RuleFor(r => r.Lines)
                .Must(lines => lines.Select(l => (l.ItemId, l.MinimumQuantity)).Distinct().Count() == lines.Count)
                .WithMessage("The same item is priced twice at the same quantity break.");

            RuleForEach(r => r.Lines).SetValidator(new PriceListLineValidator());
        }
    }
}
