namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.CreateProductionOrder
{
    public class CreateProductionOrderCommandValidator : AbstractValidator<CreateProductionOrderCommand>
    {
        public CreateProductionOrderCommandValidator()
        {
            RuleFor(r => r.ItemId).NotEmpty().WithMessage("What is being made?");
            RuleFor(r => r.Quantity).GreaterThan(0m).WithMessage("An order to make nothing is not an order.");

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency is the run costed in?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.PlannedStartDate).NotEqual(default(DateOnly)).WithMessage("When does it start?");
            RuleFor(r => r.DueDate).NotEqual(default(DateOnly)).WithMessage("When is it needed?");

            RuleFor(r => r.DueDate)
                .GreaterThanOrEqualTo(r => r.PlannedStartDate)
                .WithMessage("An order cannot be due before it starts.");

            RuleFor(r => r.Priority).IsInEnum();

            RuleFor(r => r.ReferenceId)
                .NotEmpty()
                .WithMessage("A source needs both a type and an identifier.")
                .When(r => !string.IsNullOrWhiteSpace(r.ReferenceType));

            RuleFor(r => r.ReferenceType)
                .NotEmpty()
                .WithMessage("A source needs both a type and an identifier.")
                .When(r => !string.IsNullOrWhiteSpace(r.ReferenceId));
        }
    }
}
