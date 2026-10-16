using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Application.UseCases.Modules.ProductionManagement.DTOs
{
    /// <summary>
    /// Raises a works order - صدور سفارش تولید. The recipe in effect on the start date is attached
    /// as the order is created, so a run made today and one made last year use the version each
    /// was actually built to.
    /// </summary>
    public record CreateProductionOrderDto
    {
        public Guid ItemId { get; init; }

        public decimal Quantity { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public DateOnly PlannedStartDate { get; init; }

        public DateOnly DueDate { get; init; }

        public ProductionOrderPriority Priority { get; init; } = ProductionOrderPriority.Normal;

        /// <summary>
        /// Optional. Left empty the approved bill of materials in effect on the start date is
        /// used, which is the one somebody signed off rather than the one that happens to be open.
        /// </summary>
        public Guid? BillOfMaterialsId { get; init; }

        public Guid? RoutingId { get; init; }

        /// <summary>Where the finished goods are received into.</summary>
        public Guid? WarehouseId { get; init; }

        public Guid? CostCenterId { get; init; }

        public Guid? UnitOfMeasureId { get; init; }

        public Guid? BranchId { get; init; }

        /// <summary>What caused this order, such as a sales order or a planning run.</summary>
        public string? ReferenceType { get; init; }

        public string? ReferenceId { get; init; }
    }

    /// <summary>Hands a planned order to the shop floor.</summary>
    public record ReleaseProductionOrderDto
    {
        public Guid ProductionOrderId { get; init; }

        /// <summary>
        /// Starts it in the same step. Shops that release and begin together would otherwise
        /// leave every order sitting in released, which tells a scheduler nothing.
        /// </summary>
        public bool StartImmediately { get; init; }
    }

    /// <summary>
    /// Closes a works order and receives what it made into stock - تکمیل سفارش تولید.
    /// </summary>
    public record CompleteProductionOrderDto
    {
        public Guid ProductionOrderId { get; init; }

        public decimal ProducedQuantity { get; init; }

        /// <summary>What came off the line unusable. It is costed but never becomes stock.</summary>
        public decimal ScrappedQuantity { get; init; }

        /// <summary>
        /// Where the finished goods go. Left empty the warehouse named on the order is used.
        /// </summary>
        public Guid? WarehouseId { get; init; }

        public Guid? StorageLocationId { get; init; }

        public Guid? ItemBatchId { get; init; }

        /// <summary>Labour booked to the run, over and above the material it consumed.</summary>
        public decimal LabourCost { get; init; }

        public decimal OverheadCost { get; init; }

        public string? Description { get; init; }
    }

    public sealed record ProductionOrderDto(
        Guid Id,
        string Code,
        Guid ItemId,
        string? ItemName,
        decimal Quantity,
        decimal ProducedQuantity,
        decimal ScrappedQuantity,
        decimal RemainingQuantity,
        decimal? YieldPercentage,
        ProductionOrderStatus Status,
        ProductionOrderPriority Priority,
        DateOnly? PlannedStartDate,
        DateOnly? DueDate,
        DateTimeOffset? StartedDate,
        DateTimeOffset? CompletedDate,
        Guid? BillOfMaterialsId,
        Guid? WarehouseId,
        string CurrencyCode,
        decimal MaterialCost,
        decimal LabourCost,
        decimal OverheadCost,
        decimal TotalCost,
        decimal UnitCost);
}
