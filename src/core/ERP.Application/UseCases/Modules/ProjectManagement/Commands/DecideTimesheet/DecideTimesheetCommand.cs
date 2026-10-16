using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Commands.DecideTimesheet
{
    public sealed record DecideTimesheetCommand : ICommand<Result<TimesheetDto, Error>>
    {
        public Guid TimesheetId { get; init; }

        public bool IsApproved { get; init; }

        public Guid ApprovedByHumanId { get; init; }

        public string? Reason { get; init; }
    }
}
