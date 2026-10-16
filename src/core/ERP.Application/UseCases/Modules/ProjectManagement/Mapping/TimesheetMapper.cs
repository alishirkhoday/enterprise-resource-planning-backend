using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;
using ERP.Domain.Entities.Modules.ProjectManagement;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Mapping
{
    public static class TimesheetMapper
    {
        public static TimesheetDto ToDto(this Timesheet timesheet)
        {
            ArgumentNullException.ThrowIfNull(timesheet, nameof(timesheet));

            return new TimesheetDto(
                timesheet.Id,
                timesheet.HumanId,
                timesheet.Human.FullName,
                timesheet.Period.StartDate,
                timesheet.Period.EndDate ?? timesheet.Period.StartDate,
                timesheet.Status,
                timesheet.TotalHours,
                timesheet.BillableHours,
                [.. timesheet.Entries
                    .OrderBy(e => e.WorkDate)
                    .Select(e => new TimesheetEntryViewDto(
                        e.Id,
                        e.WorkDate,
                        e.Hours,
                        e.ProjectId,
                        e.ProjectTaskId,
                        e.IsBillable,
                        e.Description))]);
        }

        public static ProjectMilestoneDto ToDto(this ProjectMilestone milestone)
        {
            ArgumentNullException.ThrowIfNull(milestone, nameof(milestone));

            return new ProjectMilestoneDto(
                milestone.Id,
                milestone.ProjectId,
                milestone.PhaseId,
                milestone.Title,
                milestone.DueDate,
                milestone.AchievedOn,
                milestone.Status,
                milestone.BillableAmount?.Amount,
                milestone.BillableAmount?.CurrencyCode,
                milestone.InvoiceId);
        }
    }
}
