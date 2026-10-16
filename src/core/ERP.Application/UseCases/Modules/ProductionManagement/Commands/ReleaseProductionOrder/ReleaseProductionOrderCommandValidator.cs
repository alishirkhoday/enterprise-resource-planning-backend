namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.ReleaseProductionOrder
{
    public class ReleaseProductionOrderCommandValidator : AbstractValidator<ReleaseProductionOrderCommand>
    {
        public ReleaseProductionOrderCommandValidator()
        {
            RuleFor(r => r.ProductionOrderId).NotEmpty().WithMessage("Which order?");
        }
    }
}
