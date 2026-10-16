namespace ERP.Application.UseCases.Modules.PlanningManagement.Commands.ReleasePlannedOrder
{
    public class ReleasePlannedOrderCommandValidator : AbstractValidator<ReleasePlannedOrderCommand>
    {
        public ReleasePlannedOrderCommandValidator()
        {
            RuleFor(r => r.PlanId).NotEmpty().WithMessage("Which plan?");
            RuleFor(r => r.LineIds).NotEmpty().WithMessage("Which suggestions are being released?");

            RuleFor(r => r.LineIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("The same line is named more than once.")
                .When(r => r.LineIds.Count > 0);
        }
    }
}
