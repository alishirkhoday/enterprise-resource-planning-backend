namespace ERP.Application.UseCases.Modules.PlanningManagement.Commands.RunMaterialRequirementPlan
{
    public class RunMaterialRequirementPlanCommandValidator : AbstractValidator<RunMaterialRequirementPlanCommand>
    {
        /// <summary>
        /// Beyond about three years a forecast is a guess dressed as a plan, and the run cost
        /// grows with the horizon for figures nobody will act on.
        /// </summary>
        private const int MaximumHorizonDays = 1_095;

        public RunMaterialRequirementPlanCommandValidator()
        {
            RuleFor(p => p.HorizonStart).NotEqual(default(DateOnly)).WithMessage("The horizon needs a start.");

            RuleFor(p => p.HorizonEnd)
                .GreaterThan(p => p.HorizonStart)
                .WithMessage("A horizon cannot end before it begins.");

            RuleFor(p => p)
                .Must(p => p.HorizonEnd.DayNumber - p.HorizonStart.DayNumber <= MaximumHorizonDays)
                .WithMessage($"A planning horizon longer than {MaximumHorizonDays} days is a forecast, not a plan.")
                .When(p => p.HorizonEnd > p.HorizonStart);

            RuleFor(p => p.ItemIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("The same item is named more than once.")
                .When(p => p.ItemIds.Count > 0);
        }
    }
}
