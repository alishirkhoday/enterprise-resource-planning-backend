using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Queries.GetAttendanceSummary
{
    /// <summary>One person's attendance over a period - کارکرد.</summary>
    public sealed record GetAttendanceSummaryQuery : IQuery<Result<AttendanceSummaryDto, Error>>
    {
        public Guid HumanId { get; init; }

        public DateOnly From { get; init; }

        public DateOnly To { get; init; }
    }
}
