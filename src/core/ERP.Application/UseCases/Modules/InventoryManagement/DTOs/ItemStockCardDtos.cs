using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.DTOs
{
    /// <summary>
    /// The stock card - کاردکس کالا: every movement of one item in one warehouse over a period,
    /// with the balance it left behind, opened and closed by the balances either side of the window.
    /// </summary>
    public sealed record ItemStockCardDto(
        Guid ItemId,
        string? ItemName,
        string? StockKeepingUnit,
        Guid WarehouseId,
        string WarehouseName,
        DateOnly From,
        DateOnly To,
        string CurrencyCode,
        decimal OpeningQuantity,
        decimal OpeningValue,
        decimal TotalIn,
        decimal TotalOut,
        decimal ClosingQuantity,
        decimal ClosingValue,
        IReadOnlyList<ItemStockCardLineDto> Movements);

    public sealed record ItemStockCardLineDto(
        Guid Id,
        DateTimeOffset MovementDateTime,
        StockMovementType MovementType,
        decimal QuantityIn,
        decimal QuantityOut,
        decimal UnitCost,
        decimal ValueChange,
        decimal BalanceQuantity,
        decimal BalanceValue,
        string? ReferenceType,
        string? ReferenceNumber,
        Guid? ItemBatchId,
        string? Description);
}
