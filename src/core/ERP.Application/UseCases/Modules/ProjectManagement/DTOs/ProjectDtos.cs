using ERP.Domain.Entities.Modules.ProjectManagement;

namespace ERP.Application.UseCases.Modules.ProjectManagement.DTOs
{
    public record SubmitTimesheetDto
    {
        public Guid HumanId { get; init; }

        public DateOnly PeriodStart { get; init; }

        public DateOnly PeriodEnd { get; init; }

        public IReadOnlyList<TimesheetEntryDto> Entries { get; init; } = [];
    }

    /// <summary>
    /// One block of time. A line against a project task is chargeable work; one against an
    /// activity type is everything else somebody's day contains.
    /// </summary>
    public record TimesheetEntryDto
    {
        public DateOnly WorkDate { get; init; }

        public decimal Hours { get; init; }

        /// <summary>The task worked on. Empty means the line is internal time.</summary>
        public Guid? ProjectTaskId { get; init; }

        /// <summary>Used when no task is named: training, leave, administration.</summary>
        public TimesheetActivityType? ActivityType { get; init; }

        public decimal? BillingRate { get; init; }

        public bool IsBillable { get; init; } = true;

        public Guid? CostCenterId { get; init; }

        public string? Description { get; init; }
    }

    public sealed record TimesheetDto(
        Guid Id,
        Guid HumanId,
        string HumanFullName,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        TimesheetStatus Status,
        decimal TotalHours,
        decimal BillableHours,
        IReadOnlyList<TimesheetEntryViewDto> Entries);

    public sealed record TimesheetEntryViewDto(
        Guid Id,
        DateOnly WorkDate,
        decimal Hours,
        Guid? ProjectId,
        Guid? ProjectTaskId,
        bool IsBillable,
        string? Description);

    public sealed record ProjectMilestoneDto(
        Guid Id,
        Guid ProjectId,
        Guid PhaseId,
        string Title,
        DateOnly DueDate,
        DateOnly? AchievedOn,
        MilestoneStatus Status,
        decimal? BillableAmount,
        string? CurrencyCode,
        Guid? InvoiceId);

    /// <summary>
    /// What a project has cost and earned so far - صورت وضعیت. Committed hours are separated from
    /// billable ones, because the gap between them is where a fixed-price project loses money.
    /// </summary>
    public sealed record ProjectStatementDto(
        Guid ProjectId,
        string Code,
        string Name,
        ProjectStatus Status,
        DateOnly From,
        DateOnly To,
        string CurrencyCode,
        decimal BudgetedCost,
        decimal ActualCost,
        decimal InvoicedAmount,
        decimal ApprovedHours,
        decimal ApprovedBillableHours,
        decimal BillableMilestoneValue,
        IReadOnlyList<ProjectMilestoneDto> BillableMilestones);
}
