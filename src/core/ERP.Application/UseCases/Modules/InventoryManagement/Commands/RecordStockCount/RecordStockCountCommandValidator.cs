using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.RecordStockCount
{
    public class RecordStockCountCommandValidator : AbstractValidator<RecordStockCountCommand>
    {
        public RecordStockCountCommandValidator()
        {
            RuleFor(r => r.StockCountId).NotEmpty().WithMessage("Which count sheet?");
            RuleFor(r => r.Lines).NotEmpty().WithMessage("A count with no lines records nothing.");

            RuleFor(r => r.Lines)
                .Must(lines => lines.Select(l => l.StockCountItemId).Distinct().Count() == lines.Count)
                .WithMessage("The same line is counted twice in one request.");

            RuleForEach(r => r.Lines).SetValidator(new StockCountLineValidator());
        }
    }

    public class StockCountLineValidator : AbstractValidator<StockCountLineDto>
    {
        public StockCountLineValidator()
        {
            RuleFor(l => l.StockCountItemId).NotEmpty().WithMessage("Every line names a sheet line.");

            // Zero is a real answer here. An empty shelf is the finding most worth recording.
            RuleFor(l => l.CountedQuantity).GreaterThanOrEqualTo(0m).WithMessage("A count cannot be negative.");

            RuleFor(l => l.DiscrepancyReason).MaximumLength(500);
        }
    }
}
