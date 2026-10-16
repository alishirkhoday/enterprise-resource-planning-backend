using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs
{
    /// <summary>
    /// Opens somebody's leave balance for a year - تخصیص مرخصی.
    /// <para>
    /// Without this, a leave request has nothing to check itself against: the balance had to be
    /// seeded by hand, so the safeguard against somebody taking leave they had not earned was
    /// only as good as whoever last touched the database.
    /// </para>
    /// </summary>
    public record GrantLeaveEntitlementDto
    {
        public Guid HumanId { get; init; }

        public HumanRequestLeaveType LeaveType { get; init; }

        public int Year { get; init; }

        public decimal EntitledDays { get; init; }

        /// <summary>Days carried over from last year, where the policy allows it.</summary>
        public decimal CarriedOverDays { get; init; }
    }

    public sealed record LeaveEntitlementDto(
        Guid Id,
        Guid HumanId,
        string EmployeeName,
        HumanRequestLeaveType LeaveType,
        int Year,
        decimal EntitledDays,
        decimal CarriedOverDays,
        decimal UsedDays,
        decimal PendingDays,
        decimal RemainingDays);

    /// <summary>
    /// What one person's attendance looks like over a period - کارکرد. The figure payroll is
    /// paid against, and the one an employee disputes, so it reports the days rather than a total.
    /// </summary>
    public sealed record AttendanceSummaryDto(
        Guid HumanId,
        string EmployeeName,
        DateOnly From,
        DateOnly To,
        int RecordedDays,
        int CompleteDays,

        /// <summary>
        /// Days the clock saw somebody arrive but never leave. Counted separately because they
        /// contribute no hours, and a total that silently skipped them would understate the
        /// month without anybody being able to see why.
        /// </summary>
        int IncompleteDays,

        decimal TotalWorkedHours,
        IReadOnlyList<AttendanceDayDto> Days);

    public sealed record AttendanceDayDto(
        Guid Id,
        DateOnly Date,
        TimeOnly? EntryTime,
        TimeOnly? ExitTime,
        decimal? WorkedHours,
        bool IsComplete);

    /// <summary>
    /// Contracts about to lapse - سررسید قرارداد. Somebody working with no agreement behind them
    /// is the one thing nobody spots until payroll fails.
    /// </summary>
    public sealed record ExpiringContractDto(
        Guid ContractId,
        Guid HumanId,
        string EmployeeName,
        string? EmployeeNumber,
        string? PositionTitle,
        HumanEmploymentType EmploymentType,
        DateOnly StartDate,
        DateOnly EndDate,
        int DaysRemaining);
}
