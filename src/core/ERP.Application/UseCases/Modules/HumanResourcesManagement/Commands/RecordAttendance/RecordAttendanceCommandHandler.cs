using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RecordAttendance
{
    /// <summary>
    /// Clocks somebody in or out.
    /// <para>
    /// One day is one record, opened on entry and closed on exit. A second call for the same day
    /// fills in the half that was missing rather than starting again, because a person who leaves
    /// for lunch and comes back has not begun a second working day.
    /// </para>
    /// </summary>
    public class RecordAttendanceCommandHandler(
        IHumanRepository humans,
        IAttendanceRepository attendance)
        : IRequestHandler<RecordAttendanceCommand, Result<AttendanceRecordDto, Error>>
    {
        public async Task<Result<AttendanceRecordDto, Error>> Handle(RecordAttendanceCommand request, CancellationToken cancellationToken)
        {
            var human = await humans.GetByIdAsync(request.HumanId, cancellationToken);
            if (human is null)
                return Result.Failure<AttendanceRecordDto, Error>(Errors.General.NotFound(request.HumanId.ToString(), nameof(Human)));

            var record = await attendance.GetForDayAsync(request.HumanId, request.Date, cancellationToken);

            if (record is null)
            {
                record = new HumanAttendanceRecord(human, request.Date);
                await attendance.AddAsync(record, cancellationToken);
            }

            if (request.EntryTime is not null)
                record.SetEntryTime(request.EntryTime.Value);

            if (request.ExitTime is not null)
            {
                // Checked against what is stored rather than what arrived, because an exit usually
                // comes in on its own, hours after the entry it has to follow.
                if (record.EntryTime is not null && request.ExitTime.Value < record.EntryTime.Value)
                    return Result.Failure<AttendanceRecordDto, Error>(Errors.HumanResources.ExitBeforeEntry(request.Date));

                record.SetExitTime(request.ExitTime.Value);
            }

            return Result.Success<AttendanceRecordDto, Error>(new AttendanceRecordDto(
                record.Id,
                record.HumanId,
                record.Date,
                record.EntryTime,
                record.ExitTime));
        }
    }
}
