using ERP.Domain.Entities.Modules.PlanningManagement;

namespace ERP.Application.UseCases.Modules.PlanningManagement.DTOs
{
    public record RunMaterialRequirementPlanDto
    {
        public DateOnly HorizonStart { get; init; }

        public DateOnly HorizonEnd { get; init; }

        /// <summary>
        /// The forecast to read demand from. Left empty the run uses firm demand only, which is
        /// what a make-to-order shop wants and what a make-to-stock one should never do.
        /// </summary>
        public Guid? DemandForecastId { get; init; }

        public Guid? WarehouseId { get; init; }

        public Guid? BranchId { get; init; }

        /// <summary>
        /// Items to plan. Empty means everything with a reorder policy, which is the usual case
        /// but also the slowest, so it is a deliberate choice rather than the only option.
        /// </summary>
        public IReadOnlyList<Guid> ItemIds { get; init; } = [];

        public string? Description { get; init; }
    }

    public sealed record MaterialRequirementPlanDto(
        Guid Id,
        string Code,
        DateOnly HorizonStart,
        DateOnly HorizonEnd,
        MaterialRequirementPlanStatus Status,
        DateTimeOffset StartedOn,
        DateTimeOffset? CompletedOn,
        int LineCount,
        int ShortageCount,
        IReadOnlyList<MaterialRequirementPlanLineDto> Shortages);

    public sealed record MaterialRequirementPlanLineDto(
        Guid Id,
        Guid ItemId,
        string? ItemName,
        DateOnly RequiredOn,
        decimal GrossRequirement,
        decimal ScheduledReceipts,
        decimal ProjectedOnHand,
        decimal NetRequirement,
        decimal PlannedOrderQuantity,
        PlannedOrderAction SuggestedAction,
        DateOnly SuggestedReleaseDate,
        bool IsReleased);

    /// <summary>Turns a planning suggestion into a real order.</summary>
    public record ReleasePlannedOrderDto
    {
        public Guid PlanId { get; init; }

        public IReadOnlyList<Guid> LineIds { get; init; } = [];
    }

    public sealed record ReleasePlannedOrderResultDto(
        Guid PlanId,
        int ReleasedCount,
        IReadOnlyList<ReleasedLineDto> Released);

    public sealed record ReleasedLineDto(
        Guid LineId,
        Guid ItemId,
        decimal Quantity,
        PlannedOrderAction Action,
        string ReferenceType,
        string ReferenceId);
}
