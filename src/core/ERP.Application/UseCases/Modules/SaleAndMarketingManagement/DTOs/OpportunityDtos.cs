using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs
{
    /// <summary>A deal being worked - فرصت فروش.</summary>
    public record CreateOpportunityDto
    {
        public string Title { get; init; } = default!;

        public decimal EstimatedValue { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public DateOnly ExpectedCloseDate { get; init; }

        /// <summary>
        /// One of these two is required. A deal with neither belongs to nobody, and a pipeline
        /// full of those forecasts revenue from thin air.
        /// </summary>
        public Guid? CustomerId { get; init; }

        public Guid? SourceLeadId { get; init; }

        public Guid? CampaignId { get; init; }

        public Guid? AssignedToHumanId { get; init; }

        public Guid? BranchId { get; init; }

        public string? CompetitorName { get; init; }

        public string? Description { get; init; }
    }

    /// <summary>Moves a deal along the pipeline - جابه‌جایی مرحله.</summary>
    public record AdvanceOpportunityDto
    {
        public Guid OpportunityId { get; init; }

        public OpportunityStage Stage { get; init; }

        /// <summary>
        /// Optional. Left empty the probability the stage implies is used, which is the number
        /// the business agreed on rather than the one the salesperson feels like today.
        /// </summary>
        public decimal? Probability { get; init; }

        /// <summary>Revises the estimate as the deal becomes better understood.</summary>
        public decimal? EstimatedValue { get; init; }

        public DateOnly? ExpectedCloseDate { get; init; }

        public string? CompetitorName { get; init; }
    }

    /// <summary>Closes a deal, won or lost - بستن فرصت.</summary>
    public record CloseOpportunityDto
    {
        public Guid OpportunityId { get; init; }

        public bool IsWon { get; init; }

        public DateOnly ClosedOn { get; init; }

        /// <summary>Required on a loss. Lost reasons are the only free lesson in a sales system.</summary>
        public string? LostReason { get; init; }

        /// <summary>The order the win turned into, when there is one.</summary>
        public Guid? SaleOrderId { get; init; }
    }

    public sealed record OpportunityDto(
        Guid Id,
        string Title,
        OpportunityStage Stage,
        Guid? CustomerId,
        string? CustomerName,
        Guid? SourceLeadId,
        string CurrencyCode,
        decimal EstimatedValue,
        decimal Probability,
        decimal WeightedValue,
        DateOnly ExpectedCloseDate,
        DateOnly? ActualCloseDate,
        Guid? CampaignId,
        Guid? AssignedToHumanId,
        Guid? QuotationId,
        Guid? SaleOrderId,
        string? CompetitorName,
        string? LostReason,
        bool IsOpen);

    /// <summary>
    /// The pipeline - قیف فروش: what is open, at which stage, and what it is worth once
    /// probability is taken into account.
    /// </summary>
    public sealed record SalesPipelineDto(
        DateOnly AsOf,
        string CurrencyCode,
        int OpenCount,
        decimal TotalEstimatedValue,
        decimal TotalWeightedValue,
        int OverdueCount,
        IReadOnlyList<PipelineStageDto> Stages);

    public sealed record PipelineStageDto(
        OpportunityStage Stage,
        int Count,
        decimal EstimatedValue,
        decimal WeightedValue);
}
