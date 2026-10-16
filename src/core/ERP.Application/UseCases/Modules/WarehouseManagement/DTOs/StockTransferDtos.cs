using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.DTOs
{
    /// <summary>A transfer request - حواله انتقال. Nothing moves until it is approved and dispatched.</summary>
    public record CreateStockTransferDto
    {
        public Guid SourceWarehouseId { get; init; }

        public Guid DestinationWarehouseId { get; init; }

        public DateOnly RequestDate { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public IReadOnlyList<StockTransferLineDto> Lines { get; init; } = [];

        public DateOnly? ExpectedArrivalDate { get; init; }

        public Guid? RequestedByHumanId { get; init; }

        public string? VehicleNumber { get; init; }

        public string? Description { get; init; }
    }

    public record StockTransferLineDto
    {
        public Guid ItemId { get; init; }

        public decimal Quantity { get; init; }

        public Guid UnitOfMeasureId { get; init; }

        /// <summary>
        /// Optional. Left empty the cost comes from what the source warehouse is carrying the
        /// stock at, which is what a transfer should move: a transfer is not a sale and must not
        /// change the value of the goods.
        /// </summary>
        public decimal? UnitCost { get; init; }

        public Guid? ItemBatchId { get; init; }

        public Guid? SourceStorageLocationId { get; init; }

        public Guid? DestinationStorageLocationId { get; init; }
    }

    public record ApproveStockTransferDto
    {
        public Guid TransferId { get; init; }
    }

    /// <summary>Goods leaving the source. After this they belong to neither warehouse.</summary>
    public record DispatchStockTransferDto
    {
        public Guid TransferId { get; init; }

        public DateOnly DispatchDate { get; init; }

        /// <summary>
        /// What actually went on the truck. An empty list dispatches every line in full, which is
        /// the common case.
        /// </summary>
        public IReadOnlyList<StockTransferMovementLineDto> Lines { get; init; } = [];

        public string? VehicleNumber { get; init; }
    }

    /// <summary>Goods arriving at the destination, and what is missing if any is.</summary>
    public record ReceiveStockTransferDto
    {
        public Guid TransferId { get; init; }

        public DateOnly ReceiveDate { get; init; }

        /// <summary>An empty list receives everything that was dispatched.</summary>
        public IReadOnlyList<StockTransferMovementLineDto> Lines { get; init; } = [];
    }

    public record StockTransferMovementLineDto
    {
        public Guid TransferItemId { get; init; }

        public decimal Quantity { get; init; }

        /// <summary>Required on receipt when less arrived than was sent.</summary>
        public string? DiscrepancyReason { get; init; }
    }

    public sealed record StockTransferDto(
        Guid Id,
        string Number,
        Guid SourceWarehouseId,
        string SourceWarehouseName,
        Guid DestinationWarehouseId,
        string DestinationWarehouseName,
        StockTransferStatus Status,
        DateOnly RequestDate,
        DateOnly? ExpectedArrivalDate,
        DateOnly? DispatchedOn,
        DateOnly? ReceivedOn,
        string CurrencyCode,
        bool HasDiscrepancy,
        IReadOnlyList<StockTransferLineViewDto> Lines);

    public sealed record StockTransferLineViewDto(
        Guid Id,
        short LineNumber,
        Guid ItemId,
        string? ItemName,
        decimal RequestedQuantity,
        decimal DispatchedQuantity,
        decimal ReceivedQuantity,
        decimal ShortageQuantity,
        decimal UnitCost,
        string? DiscrepancyReason);
}
