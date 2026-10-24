using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;
using ERP.Application.UseCases.Modules.ProjectManagement.Mapping;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.ProjectManagement;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Commands.SubmitTimesheet
{
    /// <summary>
    /// Fills in a timesheet for a period and submits it for approval.
    /// <para>
    /// Resubmitting the same period replaces what was there rather than adding to it. Somebody
    /// correcting Tuesday expects Tuesday to be corrected, not counted twice - and a timesheet
    /// that quietly accumulates is how a week ends up with sixty billed hours in it.
    /// </para>
    /// </summary>
    public class SubmitTimesheetCommandHandler(
        IHumanRepository humans,
        ITimesheetRepository timesheets,
        IProjectRepository projects)
        : IRequestHandler<SubmitTimesheetCommand, Result<TimesheetDto, Error>>
    {
        public async Task<Result<TimesheetDto, Error>> Handle(SubmitTimesheetCommand request, CancellationToken cancellationToken)
        {
            var human = await humans.GetByIdAsync(request.HumanId, cancellationToken);
            if (human is null)
                return Result.Failure<TimesheetDto, Error>(Errors.General.NotFound(request.HumanId.ToString(), nameof(Human)));

            var timesheet = await timesheets.GetByHumanAndPeriodAsync(
                request.HumanId,
                request.PeriodStart,
                request.PeriodEnd,
                cancellationToken);

            var isNew = timesheet is null;

            if (timesheet is null)
            {
                timesheet = new Timesheet(human, DateRange.Create(request.PeriodStart, request.PeriodEnd));
            }
            else if (timesheet.Status == TimesheetStatus.Approved)
            {
                return Result.Failure<TimesheetDto, Error>(Errors.Projects.TimesheetIsAlreadyApproved(request.PeriodStart, request.PeriodEnd));
            }
            else
            {
                // A rejected sheet has to be reopened before it can take entries again; a
                // submitted one is still editable until somebody decides on it.
                if (timesheet.Status == TimesheetStatus.Rejected)
                    timesheet.Reopen();

                foreach (var existing in timesheet.Entries.ToArray())
                {
                    timesheet.RemoveEntry(existing);
                }
            }

            var built = await BuildEntriesAsync(timesheet, request, cancellationToken);
            if (built.IsFailure)
                return Result.Failure<TimesheetDto, Error>(built.Error);

            try
            {
                timesheet.Submit();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<TimesheetDto, Error>(Errors.Projects.TimesheetCannotBeSubmitted(exception.Message));
            }

            if (isNew)
                await timesheets.CreateAsync(timesheet, cancellationToken);

            return Result.Success<TimesheetDto, Error>(timesheet.ToDto());
        }

        private async Task<Result<List<TimesheetEntry>, Error>> BuildEntriesAsync(
            Timesheet timesheet,
            SubmitTimesheetCommand request,
            CancellationToken cancellationToken)
        {
            var built = new List<TimesheetEntry>(request.Entries.Count);

            foreach (var line in request.Entries)
            {
                TimesheetEntry entry;

                if (line.ProjectTaskId is not null)
                {
                    var task = await projects.GetTaskAsync(line.ProjectTaskId.Value, cancellationToken);
                    if (task is null)
                        return Result.Failure<List<TimesheetEntry>, Error>(
                            Errors.General.NotFound(line.ProjectTaskId.Value.ToString(), nameof(ProjectTask)));

                    entry = new TimesheetEntry(timesheet, line.WorkDate, line.Hours, task);
                    timesheet.AddEntry(entry);
                }
                else
                {
                    // No task means internal time, and internal time is never billable whatever
                    // the request says - there is no customer to bill it to.
                    entry = new TimesheetEntry(timesheet, line.WorkDate, line.Hours, line.ActivityType!.Value);
                    timesheet.AddEntry(entry);
                    timesheet.MarkTimesheetEntryAsNonBillable(entry.Id);
                }

                if (line.ProjectTaskId is not null && !line.IsBillable)
                    timesheet.MarkTimesheetEntryAsNonBillable(entry.Id);

                if (line.BillingRate is not null)
                    timesheet.SetTimesheetEntryBillingRate(entry.Id, Money.Create(line.BillingRate.Value, "IRR"));

                if (line.CostCenterId is not null)
                    timesheet.AssignTimesheetEntryCostCenter(entry.Id, line.CostCenterId.Value);

                timesheet.SetTimesheetEntryDescription(entry.Id, line.Description);
                built.Add(entry);
            }

            return Result.Success<List<TimesheetEntry>, Error>(built);
        }
    }
}
