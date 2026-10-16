using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RecordAttendance
{
    public record RecordAttendanceCommand : RecordAttendanceDto, ICommand<Result<AttendanceRecordDto, Error>>;
}
