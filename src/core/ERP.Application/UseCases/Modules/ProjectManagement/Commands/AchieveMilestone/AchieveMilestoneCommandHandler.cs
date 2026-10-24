using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;
using ERP.Application.UseCases.Modules.ProjectManagement.Mapping;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Repositories.Modules.ProjectManagement;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Commands.AchieveMilestone
{
    /// <summary>
    /// Marks a milestone as reached. Whether that counts as late is the milestone's own judgement
    /// against its due date, not something the caller gets to assert.
    /// </summary>
    public class AchieveMilestoneCommandHandler(IProjectRepository projects)
        : IRequestHandler<AchieveMilestoneCommand, Result<ProjectMilestoneDto, Error>>
    {
        public async Task<Result<ProjectMilestoneDto, Error>> Handle(AchieveMilestoneCommand request, CancellationToken cancellationToken)
        {
            var milestone = await projects.GetMilestoneAsync(request.MilestoneId, cancellationToken);
            if (milestone is null)
                return Result.Failure<ProjectMilestoneDto, Error>(Errors.General.NotFound(request.MilestoneId.ToString(), nameof(ProjectMilestone)));

            try
            {
                milestone.Phase.Project.AchieveProjectMilestone(milestone.Id, request.AchievedOn);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<ProjectMilestoneDto, Error>(Errors.Projects.MilestoneCannotBeAchieved(exception.Message));
            }

            return Result.Success<ProjectMilestoneDto, Error>(milestone.ToDto());
        }
    }
}
