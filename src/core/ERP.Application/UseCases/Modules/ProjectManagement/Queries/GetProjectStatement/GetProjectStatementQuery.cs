using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Queries.GetProjectStatement
{
    /// <summary>
    /// What a project has cost and earned. The window defaults to the project's own planned
    /// period, which is what somebody means when they ask how a project is doing.
    /// </summary>
    public sealed record GetProjectStatementQuery : IQuery<Result<ProjectStatementDto, Error>>
    {
        public Guid ProjectId { get; init; }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }
    }
}
