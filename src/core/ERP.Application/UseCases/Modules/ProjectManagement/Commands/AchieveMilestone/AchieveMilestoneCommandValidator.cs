namespace ERP.Application.UseCases.Modules.ProjectManagement.Commands.AchieveMilestone
{
    public class AchieveMilestoneCommandValidator : AbstractValidator<AchieveMilestoneCommand>
    {
        public AchieveMilestoneCommandValidator()
        {
            RuleFor(m => m.MilestoneId).NotEmpty().WithMessage("Which milestone?");
            RuleFor(m => m.AchievedOn).NotEqual(default(DateOnly)).WithMessage("When was it reached?");
        }
    }
}
