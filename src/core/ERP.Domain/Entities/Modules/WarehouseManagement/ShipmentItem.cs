using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// One line of a shipment: what went on the vehicle.
/// </summary>
public sealed class ShipmentItem : BaseEntity
{
    public Shipment Shipment { get; private set; } = default!;
    public Guid ShipmentId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>The order line this shipment satisfies, so delivery can be posted back to it.</summary>
    public Guid? SaleOrderItemId { get; private set; }

    public Guid? ItemBatchId { get; private set; }
    public Guid? SerialNumberId { get; private set; }
    public Guid? StorageLocationId { get; private set; }
    public string? PackageReference { get; private set; }

    private ShipmentItem() { }

    public ShipmentItem(Shipment shipment, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId)
    {
        ArgumentNullException.ThrowIfNull(shipment, nameof(shipment));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        Shipment = shipment;
        ShipmentId = shipment.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
    }

    internal void LinkToSaleOrderItem(Guid saleOrderItemId) =>
        SaleOrderItemId = saleOrderItemId == Guid.Empty ? null : saleOrderItemId;

    internal void SetTracking(Guid? itemBatchId, Guid? serialNumberId)
    {
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
    }

    internal void ScopeToStorageLocation(Guid storageLocationId) =>
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;

    internal void SetPackageReference(string? packageReference) =>
        PackageReference = string.IsNullOrWhiteSpace(packageReference) ? null : packageReference.Trim();
}
