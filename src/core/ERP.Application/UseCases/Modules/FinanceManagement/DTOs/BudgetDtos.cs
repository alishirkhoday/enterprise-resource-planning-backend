using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.DTOs
{
    /// <summary>A budget for a financial year - بودجه.</summary>
    public record DraftBudgetDto
    {
        public Guid FinancialYearId { get; init; }

        public string Title { get; init; } = default!;

        public BudgetType Type { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public Guid? CostCenterId { get; init; }

        public Guid? BranchId { get; init; }

        public Guid? ProjectId { get; init; }

        public IReadOnlyList<BudgetLineDto> Lines { get; init; } = [];

        public string? Description { get; init; }

        /// <summary>
        /// Sends it for approval straight away. A budget left in draft controls nothing, so most
        /// requests mean to do both.
        /// </summary>
        public bool SubmitImmediately { get; init; } = true;
    }

    public record BudgetLineDto
    {
        public Guid AccountId { get; init; }

        /// <summary>Which period of the year: 1 to 12 for a monthly budget.</summary>
        public short PeriodNumber { get; init; }

        public decimal PlannedAmount { get; init; }

        public Guid? CostCenterId { get; init; }

        public string? Description { get; init; }
    }

    /// <summary>Approves or rejects a submitted budget - تأیید بودجه.</summary>
    public record DecideBudgetDto
    {
        public Guid BudgetId { get; init; }

        public bool IsApproved { get; init; }

        public string? ApprovedBy { get; init; }
    }

    /// <summary>
    /// Asks whether a spend fits - کنترل بودجه. Read only: it answers, it does not reserve.
    /// </summary>
    public sealed record BudgetCheckDto(
        Guid FinancialYearId,
        Guid AccountId,
        short PeriodNumber,
        string CurrencyCode,
        decimal Planned,
        decimal Actual,
        decimal Committed,
        decimal Available,
        decimal Requested,
        bool Fits,
        decimal WouldBe,
        bool IsOverspent);

    public sealed record BudgetDto(
        Guid Id,
        Guid FinancialYearId,
        string Title,
        BudgetType Type,
        BudgetStatus Status,
        string CurrencyCode,
        int Version,
        decimal TotalPlanned,
        decimal TotalActual,
        decimal TotalVariance,
        Guid? CostCenterId,
        Guid? ProjectId,
        DateTimeOffset? ApprovedOn,
        string? ApprovedBy,
        IReadOnlyList<BudgetLineViewDto> Lines);

    public sealed record BudgetLineViewDto(
        Guid Id,
        Guid AccountId,
        string? AccountCode,
        string? AccountName,
        short PeriodNumber,
        decimal PlannedAmount,
        decimal ActualAmount,
        decimal CommittedAmount,
        decimal Available,
        bool IsOverspent);
}
