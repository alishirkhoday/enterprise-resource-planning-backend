using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Application.UseCases.Modules.ProductionManagement.DTOs
{
    /// <summary>
    /// Material moving from the warehouse into a works order - مصرف مواد. This is the link that
    /// makes production cost anything: without it, making something consumes nothing.
    /// </summary>
    public record IssueMaterialDto
    {
        public Guid ProductionOrderId { get; init; }

        public Guid WarehouseId { get; init; }

        public DateOnly IssueDate { get; init; }

        /// <summary>
        /// What to take. An empty list issues what the order's bill of materials calls for, less
        /// whatever has already gone out, which is the common case on a first issue.
        /// </summary>
        public IReadOnlyList<MaterialIssueLineDto> Lines { get; init; } = [];

        /// <summary>The routing step the material is being issued for.</summary>
        public int? OperationSequenceNumber { get; init; }

        public Guid? IssuedByHumanId { get; init; }

        public Guid? StorageLocationId { get; init; }

        public string? Description { get; init; }
    }

    public record MaterialIssueLineDto
    {
        public Guid ComponentId { get; init; }

        public decimal Quantity { get; init; }

        public Guid? ItemBatchId { get; init; }

        public Guid? SerialNumberId { get; init; }

        public Guid? StorageLocationId { get; init; }

        public string? Description { get; init; }
    }

    public sealed record MaterialIssueDto(
        Guid Id,
        string Number,
        Guid ProductionOrderId,
        string ProductionOrderCode,
        Guid WarehouseId,
        DateOnly IssueDate,
        MaterialIssueStatus Status,
        string CurrencyCode,
        decimal TotalCost,
        IReadOnlyList<MaterialIssueLineViewDto> Lines);

    public sealed record MaterialIssueLineViewDto(
        Guid Id,
        short LineNumber,
        Guid ComponentId,
        string? ComponentName,
        decimal PlannedQuantity,
        decimal IssuedQuantity,
        decimal Variance,
        decimal UnitCost,
        decimal TotalCost);
}
