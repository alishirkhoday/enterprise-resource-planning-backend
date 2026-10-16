using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.DTOs
{
    public record CreatePurchaseOrderDto
    {
        public Guid SupplierId { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public IReadOnlyList<PurchaseOrderLineDto> Lines { get; init; } = [];

        public DateOnly? ExpectedDeliveryDate { get; init; }

        public Guid? WarehouseId { get; init; }

        public Guid? PaymentTermId { get; init; }

        public Guid? CostCenterId { get; init; }

        public Guid? BranchId { get; init; }

        /// <summary>Freight, duty and the rest, which belong to the order rather than to a line.</summary>
        public decimal AdditionalCosts { get; init; }

        public string? Description { get; init; }
    }

    public record PurchaseOrderLineDto
    {
        public Guid ItemId { get; init; }

        public decimal Quantity { get; init; }

        public Guid UnitOfMeasureId { get; init; }

        /// <summary>
        /// Optional. Left empty, the price comes from the framework agreement with this supplier,
        /// which is the point of having one.
        /// </summary>
        public decimal? UnitPrice { get; init; }

        public decimal? DiscountPercentage { get; init; }

        public Guid? TaxId { get; init; }

        public decimal? TaxPercentage { get; init; }

        public DateOnly? ExpectedDeliveryDate { get; init; }

        public string? Description { get; init; }
    }

    public sealed record PurchaseOrderDto(
        Guid Id,
        string Number,
        Guid SupplierId,
        string SupplierName,
        PurchaseOrderStatus Status,
        string CurrencyCode,
        DateOnly? ExpectedDeliveryDate,
        decimal SubTotal,
        decimal TotalTax,
        decimal AdditionalCosts,
        decimal GrandTotal,
        IReadOnlyList<PurchaseOrderLineViewDto> Lines);

    public sealed record PurchaseOrderLineViewDto(
        Guid Id,
        short LineNumber,
        Guid ItemId,
        string? ItemName,
        decimal Quantity,
        decimal ReceivedQuantity,
        decimal UnitPrice,
        decimal LineTotal);
}
