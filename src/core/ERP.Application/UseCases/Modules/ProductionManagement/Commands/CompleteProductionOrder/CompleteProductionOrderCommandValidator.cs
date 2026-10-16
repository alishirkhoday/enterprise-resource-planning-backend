namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.CompleteProductionOrder
{
    public class CompleteProductionOrderCommandValidator : AbstractValidator<CompleteProductionOrderCommand>
    {
        public CompleteProductionOrderCommandValidator()
        {
            RuleFor(r => r.ProductionOrderId).NotEmpty().WithMessage("Which order?");

            RuleFor(r => r.ProducedQuantity).GreaterThanOrEqualTo(0m).WithMessage("A quantity cannot be negative.");
            RuleFor(r => r.ScrappedQuantity).GreaterThanOrEqualTo(0m).WithMessage("A quantity cannot be negative.");

            // A run that made nothing and scrapped nothing was not a run. Recording it would
            // close an order with no account of where its material went.
            RuleFor(r => r)
                .Must(r => r.ProducedQuantity > 0m || r.ScrappedQuantity > 0m)
                .WithMessage("Say what came off the line, even if all of it was scrap.");

            RuleFor(r => r.LabourCost).GreaterThanOrEqualTo(0m).WithMessage("A cost cannot be negative.");
            RuleFor(r => r.OverheadCost).GreaterThanOrEqualTo(0m).WithMessage("A cost cannot be negative.");
        }
    }
}
