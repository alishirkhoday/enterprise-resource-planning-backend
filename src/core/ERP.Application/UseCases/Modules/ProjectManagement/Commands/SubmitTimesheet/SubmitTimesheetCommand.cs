using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Commands.SubmitTimesheet
{
    public record SubmitTimesheetCommand : SubmitTimesheetDto, ICommand<Result<TimesheetDto, Error>>;
}
