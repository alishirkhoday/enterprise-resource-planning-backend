using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs
{
    /// <summary>A quota - هدف فروش.</summary>
    public record SetSalesTargetDto
    {
        public string Title { get; init; } = default!;

        public SalesTargetScope Scope { get; init; }

        /// <summary>Whose target it is: a person, a team, a territory, a branch.</summary>
        public Guid ScopeReferenceId { get; init; }

        public DateOnly StartDate { get; init; }

        public DateOnly? EndDate { get; init; }

        public decimal TargetAmount { get; init; }

        public string CurrencyCode { get; init; } = default!;

        /// <summary>A count target alongside the value, such as ten new customers.</summary>
        public int? TargetCount { get; init; }

        /// <summary>Narrows the target to one product family.</summary>
        public Guid? ItemGroupId { get; init; }

        public Guid? BranchId { get; init; }

        public string? Description { get; init; }
    }

    public sealed record SalesTargetDto(
        Guid Id,
        string Title,
        SalesTargetScope Scope,
        Guid ScopeReferenceId,
        DateOnly StartDate,
        DateOnly? EndDate,
        string CurrencyCode,
        decimal TargetAmount,
        decimal AchievedAmount,
        decimal Shortfall,
        decimal AchievementPercentage,
        int? TargetCount,
        int AchievedCount,
        bool IsMet,
        Guid? ItemGroupId);

    /// <summary>
    /// How a salesperson is doing - کارنامه فروش: what they were asked for, what they have won,
    /// what is still in the pipeline, and what they have earned on it.
    /// </summary>
    public sealed record SalesPerformanceDto(
        Guid SalesRepHumanId,
        DateOnly From,
        DateOnly To,
        string CurrencyCode,
        decimal TargetAmount,
        decimal AchievedAmount,
        decimal AchievementPercentage,
        decimal OpenPipelineValue,
        decimal WeightedPipelineValue,
        decimal CommissionEarned,
        int OpenDealCount,
        int OpenActivityCount,
        int OverdueActivityCount,
        IReadOnlyList<SalesTargetDto> Targets);
}
