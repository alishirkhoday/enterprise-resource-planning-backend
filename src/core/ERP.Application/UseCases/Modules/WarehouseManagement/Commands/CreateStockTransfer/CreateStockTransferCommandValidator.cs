using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.CreateStockTransfer
{
    public class CreateStockTransferCommandValidator : AbstractValidator<CreateStockTransferCommand>
    {
        public CreateStockTransferCommandValidator()
        {
            RuleFor(r => r.SourceWarehouseId).NotEmpty().WithMessage("Where is it coming from?");
            RuleFor(r => r.DestinationWarehouseId).NotEmpty().WithMessage("Where is it going?");

            RuleFor(r => r.DestinationWarehouseId)
                .NotEqual(r => r.SourceWarehouseId)
                .WithMessage("A transfer to the same warehouse moves nothing.");

            RuleFor(r => r.RequestDate).NotEqual(default(DateOnly)).WithMessage("A transfer needs a date.");

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency is the stock valued in?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.ExpectedArrivalDate)
                .GreaterThanOrEqualTo(r => r.RequestDate)
                .WithMessage("Goods cannot arrive before they are asked for.")
                .When(r => r.ExpectedArrivalDate is not null);

            RuleFor(r => r.Lines).NotEmpty().WithMessage("A transfer with no lines moves nothing.");

            RuleForEach(r => r.Lines).SetValidator(new StockTransferLineValidator());
        }
    }

    public class StockTransferLineValidator : AbstractValidator<StockTransferLineDto>
    {
        public StockTransferLineValidator()
        {
            RuleFor(l => l.ItemId).NotEmpty().WithMessage("Every line names an item.");
            RuleFor(l => l.UnitOfMeasureId).NotEmpty().WithMessage("Every line states its unit.");
            RuleFor(l => l.Quantity).GreaterThan(0m).WithMessage("A line of nothing is not a line.");

            RuleFor(l => l.UnitCost)
                .GreaterThanOrEqualTo(0m).WithMessage("A cost cannot be negative.")
                .When(l => l.UnitCost is not null);
        }
    }
}
