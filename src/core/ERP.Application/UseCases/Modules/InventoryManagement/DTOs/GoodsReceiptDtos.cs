using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.DTOs
{
    public record ReceiveGoodsDto
    {
        public Guid PurchaseOrderId { get; init; }

        public Guid WarehouseId { get; init; }

        public DateOnly ReceiptDate { get; init; }

        public IReadOnlyList<ReceiveGoodsLineDto> Lines { get; init; } = [];

        public Guid? StorageLocationId { get; init; }

        public Guid? ReceivedByHumanId { get; init; }

        public string? DeliveryNoteNumber { get; init; }

        public string? VehicleNumber { get; init; }

        public string? Description { get; init; }
    }

    public record ReceiveGoodsLineDto
    {
        /// <summary>Which order line this delivery satisfies.</summary>
        public Guid PurchaseOrderItemId { get; init; }

        public decimal ReceivedQuantity { get; init; }

        /// <summary>What failed inspection. It arrived, but it never becomes stock.</summary>
        public decimal RejectedQuantity { get; init; }

        public string? RejectionReason { get; init; }

        /// <summary>
        /// What it actually cost, when the invoice differs from the order. Left empty the ordered
        /// price is used, which is right until a supplier prices a delivery differently.
        /// </summary>
        public decimal? UnitCost { get; init; }

        public Guid? ItemBatchId { get; init; }
    }

    public sealed record GoodsReceiptDto(
        Guid Id,
        string Number,
        Guid PurchaseOrderId,
        string PurchaseOrderNumber,
        Guid WarehouseId,
        DateOnly ReceiptDate,
        GoodsReceiptStatus Status,
        IReadOnlyList<GoodsReceiptLineViewDto> Lines);

    public sealed record GoodsReceiptLineViewDto(
        Guid Id,
        Guid ItemId,
        string? ItemName,
        decimal ReceivedQuantity,
        decimal AcceptedQuantity,
        decimal RejectedQuantity,
        decimal UnitCost);
}
