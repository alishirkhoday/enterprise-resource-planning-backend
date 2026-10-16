using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.DTOs
{
    /// <summary>
    /// Opens a count sheet - انبارگردانی. The book quantities are captured as the sheet is built
    /// and frozen in the same step, so nobody counts against a balance that moved underneath them.
    /// </summary>
    public record CreateStockCountDto
    {
        public Guid WarehouseId { get; init; }

        public StockCountType Type { get; init; } = StockCountType.Full;

        public DateOnly CountDate { get; init; }

        /// <summary>
        /// Which items to put on the sheet. An empty list takes everything the warehouse holds,
        /// which is what a full count means.
        /// </summary>
        public IReadOnlyList<Guid> ItemIds { get; init; } = [];

        public Guid? StorageLocationId { get; init; }

        public Guid? CountedByHumanId { get; init; }

        public Guid? BranchId { get; init; }

        public string? Description { get; init; }
    }

    /// <summary>What was actually found on the shelves.</summary>
    public record RecordStockCountDto
    {
        public Guid StockCountId { get; init; }

        public IReadOnlyList<StockCountLineDto> Lines { get; init; } = [];

        /// <summary>
        /// Sends the sheet for approval once every line has been counted. Left false the sheet
        /// stays open, which is how a count spread over several days is entered.
        /// </summary>
        public bool SubmitWhenComplete { get; init; }
    }

    public record StockCountLineDto
    {
        public Guid StockCountItemId { get; init; }

        public decimal CountedQuantity { get; init; }

        /// <summary>
        /// A second count of the same line. It replaces the first rather than averaging with it:
        /// the recount exists because somebody did not believe the first one.
        /// </summary>
        public bool IsRecount { get; init; }

        public string? DiscrepancyReason { get; init; }
    }

    /// <summary>
    /// Approves a count and writes the adjustments. This is the step that changes what the books
    /// say, so it is deliberately separate from recording what was found.
    /// </summary>
    public record ApproveStockCountDto
    {
        public Guid StockCountId { get; init; }

        public Guid ApprovedByHumanId { get; init; }

        public Guid? AdjustmentDocumentId { get; init; }
    }

    public sealed record StockCountDto(
        Guid Id,
        string Number,
        Guid WarehouseId,
        StockCountType Type,
        StockCountStatus Status,
        DateOnly CountDate,
        DateTimeOffset? FrozenOn,
        DateTimeOffset? CompletedOn,
        decimal TotalSurplus,
        decimal TotalShortage,
        decimal NetVarianceValue,
        string CurrencyCode,
        IReadOnlyList<StockCountLineViewDto> Lines);

    public sealed record StockCountLineViewDto(
        Guid Id,
        Guid ItemId,
        string? ItemName,
        decimal SystemQuantity,
        decimal? CountedQuantity,
        decimal? RecountedQuantity,
        decimal Variance,
        decimal VarianceValue,
        bool HasDiscrepancy,
        string? DiscrepancyReason);
}
