using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs
{
    /// <summary>The employment contract - حکم کارگزینی - that everything about pay hangs from.</summary>
    public record IssueEmploymentContractDto
    {
        public Guid HumanId { get; init; }

        public Guid PositionId { get; init; }

        public HumanEmploymentType EmploymentType { get; init; }

        public DateOnly StartDate { get; init; }

        /// <summary>Left empty for an open-ended contract.</summary>
        public DateOnly? EndDate { get; init; }

        public decimal BaseSalary { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public string? ContractNumber { get; init; }

        public decimal? WeeklyWorkingHours { get; init; }

        public DateOnly? ProbationEndDate { get; init; }

        public Guid? CostCenterId { get; init; }

        public Guid? WorkCalendarId { get; init; }

        public string? Description { get; init; }
    }

    public sealed record EmploymentContractDto(
        Guid Id,
        Guid HumanId,
        string HumanFullName,
        Guid PositionId,
        HumanEmploymentType EmploymentType,
        DateOnly StartDate,
        DateOnly? EndDate,
        decimal BaseSalary,
        string CurrencyCode,
        string? ContractNumber,
        DateOnly? ProbationEndDate);

    public record RecordAttendanceDto
    {
        public Guid HumanId { get; init; }

        public DateOnly Date { get; init; }

        public TimeOnly? EntryTime { get; init; }

        public TimeOnly? ExitTime { get; init; }
    }

    public sealed record AttendanceRecordDto(
        Guid Id,
        Guid HumanId,
        DateOnly Date,
        TimeOnly? EntryTime,
        TimeOnly? ExitTime);

    public record RequestLeaveDto
    {
        public Guid HumanId { get; init; }

        public HumanRequestType Type { get; init; }

        public DateOnly StartDate { get; init; }

        public DateOnly EndDate { get; init; }

        public TimeOnly? StartTime { get; init; }

        public TimeOnly? EndTime { get; init; }

        public HumanRequestLeaveType? LeaveType { get; init; }

        public string? Reason { get; init; }
    }

    public sealed record LeaveRequestDto(
        Guid Id,
        Guid HumanId,
        HumanRequestType Type,
        HumanRequestLeaveType? LeaveType,
        DateOnly StartDate,
        DateOnly EndDate,
        HumanRequestStatus Status,
        decimal? RemainingEntitlementDays);

    public record RunPayrollDto
    {
        public Guid HumanId { get; init; }

        public DateOnly StartDate { get; init; }

        public DateOnly EndDate { get; init; }

        public decimal WorkedDays { get; init; }

        public decimal AbsentDays { get; init; }

        public decimal PaidLeaveDays { get; init; }

        public decimal UnpaidLeaveDays { get; init; }

        public decimal OvertimeHours { get; init; }

        public IReadOnlyList<PayrollLineDto> AdditionalLines { get; init; } = [];

        public Guid? SalaryStructureId { get; init; }

        public Guid? CostCenterId { get; init; }

        public Guid? BranchId { get; init; }
    }

    public record PayrollLineDto
    {
        public string Code { get; init; } = default!;

        public string Title { get; init; } = default!;

        public SalaryItemNature Nature { get; init; }

        public decimal Amount { get; init; }
    }

    public sealed record PayrollDto(
        Guid Id,
        Guid HumanId,
        string HumanFullName,
        DateOnly StartDate,
        DateOnly EndDate,
        PayrollStatus Status,
        string CurrencyCode,
        decimal BaseSalary,
        decimal GrossPay,
        decimal TotalDeductions,
        decimal NetPayable,
        IReadOnlyList<PayrollLineViewDto> Lines);

    public sealed record PayrollLineViewDto(string Code, string Title, SalaryItemNature Nature, decimal Amount);
}
