using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Queries.GetAttendanceSummary
{
    /// <summary>
    /// What somebody's attendance looks like over a period - کارکرد.
    /// <para>
    /// The days are returned, not just a total. This is the figure payroll pays against and the
    /// one an employee disputes, and a single number cannot be argued with - which in practice
    /// means it is argued with anyway, by hand, from a paper register.
    /// </para>
    /// </summary>
    public class GetAttendanceSummaryQueryHandler(
        IHumanRepository humans,
        IAttendanceRepository attendance)
        : IRequestHandler<GetAttendanceSummaryQuery, Result<AttendanceSummaryDto, Error>>
    {
        public async Task<Result<AttendanceSummaryDto, Error>> Handle(GetAttendanceSummaryQuery request, CancellationToken cancellationToken)
        {
            var human = await humans.GetByIdAsync(request.HumanId, cancellationToken);
            if (human is null)
                return Result.Failure<AttendanceSummaryDto, Error>(Errors.General.NotFound(request.HumanId.ToString(), nameof(Human)));

            var records = await attendance.GetBetweenAsync(request.HumanId, request.From, request.To, cancellationToken);

            var days = records
                .OrderBy(r => r.Date)
                .Select(r => new AttendanceDayDto(
                    r.Id,
                    r.Date,
                    r.EntryTime,
                    r.ExitTime,
                    WorkedHours(r),
                    r.EntryTime is not null && r.ExitTime is not null))
                .ToArray();

            return Result.Success<AttendanceSummaryDto, Error>(new AttendanceSummaryDto(
                human.Id,
                human.FullName,
                request.From,
                request.To,
                days.Length,
                days.Count(d => d.IsComplete),
                days.Count(d => !d.IsComplete),
                days.Sum(d => d.WorkedHours ?? 0m),
                days));
        }

        /// <summary>
        /// Null rather than zero when the day is incomplete. Zero would read as somebody who came
        /// in and did nothing; null says the clock never saw them leave, which is a different
        /// problem with a different fix.
        /// </summary>
        private static decimal? WorkedHours(HumanAttendanceRecord record)
        {
            if (record.EntryTime is null || record.ExitTime is null)
                return null;

            var worked = record.ExitTime.Value - record.EntryTime.Value;

            // A negative span means the exit was recorded before the entry, which the command
            // that writes them refuses. If one ever reaches here it is data, not arithmetic.
            return worked < TimeSpan.Zero ? null : Math.Round((decimal)worked.TotalHours, 2);
        }
    }
}
