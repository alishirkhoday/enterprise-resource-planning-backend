using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;
using ERP.Application.UseCases.Modules.ProjectManagement.Mapping;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Repositories.Modules.ProjectManagement;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Commands.DecideTimesheet
{
    /// <summary>
    /// Approves or rejects a submitted timesheet.
    /// <para>
    /// Approval is what lets the hours reach a project's cost, so this is the gate between what
    /// somebody says they did and what the business bills for.
    /// </para>
    /// </summary>
    public class DecideTimesheetCommandHandler(ITimesheetRepository timesheets)
        : IRequestHandler<DecideTimesheetCommand, Result<TimesheetDto, Error>>
    {
        public async Task<Result<TimesheetDto, Error>> Handle(DecideTimesheetCommand request, CancellationToken cancellationToken)
        {
            var timesheet = await timesheets.GetWithEntriesAsync(request.TimesheetId, cancellationToken);
            if (timesheet is null)
                return Result.Failure<TimesheetDto, Error>(Errors.General.NotFound(request.TimesheetId.ToString(), nameof(Timesheet)));

            try
            {
                if (request.IsApproved)
                    timesheet.Approve(request.ApprovedByHumanId);
                else
                    timesheet.Reject(request.Reason!);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<TimesheetDto, Error>(Errors.Projects.TimesheetCannotBeDecided(exception.Message));
            }

            await timesheets.UpdateAsync(timesheet, cancellationToken);

            return Result.Success<TimesheetDto, Error>(timesheet.ToDto());
        }
    }
}
