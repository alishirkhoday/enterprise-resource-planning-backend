using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ReportingManagement.Commands.ScheduleReport
{
    public record ScheduleReportCommand : ScheduleReportDto, ICommand<Result<ScheduledReportDto, Error>>;
}
