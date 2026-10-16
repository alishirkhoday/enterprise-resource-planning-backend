using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Finished goods moving from a production order into the warehouse, valued at the cost the
/// order accumulated.
/// </summary>
public sealed class ProductionReceipt : BaseEntity
{
    public string Number { get; private set; } = default!;

    public ProductionOrder ProductionOrder { get; private set; } = default!;
    public Guid ProductionOrderId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }
    public Guid? StorageLocationId { get; private set; }

    public DateOnly ReceiptDate { get; private set; }
    public ProductionReceiptStatus Status { get; private set; }

    public decimal ReceivedQuantity { get; private set; }
    public decimal ScrappedQuantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }

    public Money UnitCost { get; private set; } = default!;
    public Money TotalCost => UnitCost.Multiply(ReceivedQuantity);

    public Guid? ItemBatchId { get; private set; }
    public Guid? WarehouseTransactionId { get; private set; }
    public Guid? ReceivedByHumanId { get; private set; }

    /// <summary>Set when the output must pass inspection before it can be sold.</summary>
    public bool RequiresQualityCheck { get; private set; }

    public string? Description { get; private set; }

    private readonly List<Guid> _serialNumberIds = [];
    public IReadOnlyList<Guid> SerialNumberIds => _serialNumberIds;

    private ProductionReceipt() { }

    public ProductionReceipt(string number, ProductionOrder productionOrder, Guid warehouseId, DateOnly receiptDate, decimal receivedQuantity, Guid unitOfMeasureId, Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(productionOrder, nameof(productionOrder));
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(receivedQuantity, nameof(receivedQuantity));
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is empty.", nameof(warehouseId));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (productionOrder.Status != ProductionOrderStatus.InProgress)
            throw new InvalidOperationException($"Output can only be received from an order in progress; this one is {productionOrder.Status}.");

        Number = number.Trim();
        ProductionOrder = productionOrder;
        ProductionOrderId = productionOrder.Id;
        Item = productionOrder.Item;
        ItemId = productionOrder.ItemId;
        WarehouseId = warehouseId;
        ReceiptDate = receiptDate;
        ReceivedQuantity = receivedQuantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitCost = unitCost;
        Status = ProductionReceiptStatus.Draft;
    }

    public void SetScrappedQuantity(decimal scrappedQuantity)
    {
        EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfNegative(scrappedQuantity, nameof(scrappedQuantity));
        ScrappedQuantity = scrappedQuantity;
    }

    public void SetBatch(Guid itemBatchId) => ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;

    public void AddSerialNumber(Guid serialNumberId)
    {
        if (Item.TrackingType != ItemTrackingType.Serial)
            throw new InvalidOperationException($"Item {Item.Name} is not serial tracked.");
        if (serialNumberId == Guid.Empty)
            throw new ArgumentException("SerialNumberId is empty.", nameof(serialNumberId));
        if (_serialNumberIds.Count >= ReceivedQuantity)
            throw new InvalidOperationException("There are already as many serials as received units.");
        _serialNumberIds.Add(serialNumberId);
    }

    public void ScopeToStorageLocation(Guid storageLocationId) =>
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;

    public void SetReceiver(Guid humanId) => ReceivedByHumanId = humanId == Guid.Empty ? null : humanId;

    public void RequireQualityCheck() => RequiresQualityCheck = true;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// Confirms the receipt and posts the output back to the order. The caller is expected to
    /// write the matching stock ledger entries in the same transaction.
    /// </summary>
    public void Confirm(Guid? warehouseTransactionId = null)
    {
        EnsureDraft();
        ProductionOrder.RecordOutput(ReceivedQuantity, ScrappedQuantity);
        WarehouseTransactionId = warehouseTransactionId == Guid.Empty ? null : warehouseTransactionId;
        Status = ProductionReceiptStatus.Confirmed;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == ProductionReceiptStatus.Confirmed)
            throw new InvalidOperationException("A confirmed receipt cannot be cancelled.");
        Status = ProductionReceiptStatus.Cancelled;
        Description = reason.Trim();
    }

    private void EnsureDraft()
    {
        if (Status != ProductionReceiptStatus.Draft)
            throw new InvalidOperationException($"The receipt is {Status} and can no longer be edited.");
    }
}
