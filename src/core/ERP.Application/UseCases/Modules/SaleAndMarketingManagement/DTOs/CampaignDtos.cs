using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs
{
    /// <summary>A marketing campaign - کمپین بازاریابی.</summary>
    public record LaunchCampaignDto
    {
        public string Code { get; init; } = default!;

        public string Name { get; init; } = default!;

        /// <summary>How it reaches people: email, instagram, a trade show.</summary>
        public string Channel { get; init; } = default!;

        public DateOnly StartDate { get; init; }

        /// <summary>Left empty the campaign runs until somebody stops it.</summary>
        public DateOnly? EndDate { get; init; }

        public decimal Budget { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public int? TargetLeadCount { get; init; }

        public Guid? CustomerGroupId { get; init; }

        public Guid? OwnerHumanId { get; init; }

        public Guid? BranchId { get; init; }

        public string? Description { get; init; }

        /// <summary>
        /// Starts it immediately. A campaign that is planned but never launched attracts no
        /// leads, so most requests mean to do both.
        /// </summary>
        public bool StartImmediately { get; init; } = true;
    }

    /// <summary>Money spent on a campaign as it is spent - ثبت هزینه کمپین.</summary>
    public record RecordCampaignCostDto
    {
        public Guid CampaignId { get; init; }

        public decimal Amount { get; init; }

        public string? Description { get; init; }
    }

    public sealed record CampaignDto(
        Guid Id,
        string Code,
        string Name,
        string Channel,
        CampaignStatus Status,
        DateOnly StartDate,
        DateOnly? EndDate,
        string CurrencyCode,
        decimal Budget,
        decimal ActualCost,
        decimal AttributedRevenue,
        int? TargetLeadCount,
        Guid? OwnerHumanId);

    /// <summary>
    /// Whether a campaign was worth running - عملکرد کمپین. The only measure that matters is
    /// revenue against cost; lead counts flatter a campaign that attracted the wrong people.
    /// </summary>
    public sealed record CampaignPerformanceDto(
        Guid CampaignId,
        string Code,
        string Name,
        CampaignStatus Status,
        string CurrencyCode,
        decimal Budget,
        decimal ActualCost,
        decimal AttributedRevenue,
        decimal NetReturn,
        decimal? ReturnOnSpendPercentage,
        int LeadCount,
        int ConvertedLeadCount,
        decimal ConversionRatePercentage,
        decimal? CostPerLead,
        decimal? CostPerConversion,
        int? TargetLeadCount,
        bool IsOverBudget);
}
