using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Commands.AchieveMilestone
{
    /// <summary>
    /// Records that a milestone has been reached. On a milestone that carries a value this is what
    /// makes it billable, so it is a commercial act as much as a progress one.
    /// </summary>
    public sealed record AchieveMilestoneCommand : ICommand<Result<ProjectMilestoneDto, Error>>
    {
        public Guid MilestoneId { get; init; }

        public DateOnly AchievedOn { get; init; }
    }
}
