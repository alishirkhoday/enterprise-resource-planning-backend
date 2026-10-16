namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One movement of one item in one warehouse, with the running quantity and value it left behind.
/// This is the inventory card: every receipt, issue and adjustment writes exactly one entry, and
/// the balance is never edited in place.
/// </summary>
public sealed class StockLedgerEntry : BaseEntity
{
    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }
    public Guid? StorageLocationId { get; private set; }
    public Guid? ItemBatchId { get; private set; }
    public Guid? SerialNumberId { get; private set; }

    public StockMovementType MovementType { get; private set; }
    public DateTimeOffset MovementDateTime { get; private set; }

    /// <summary>Positive for a receipt, negative for an issue.</summary>
    public decimal QuantityChange { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>The value the movement carried, signed the same way as the quantity.</summary>
    public Money ValueChange { get; private set; } = default!;

    public Money UnitCost { get; private set; } = default!;

    /// <summary>The quantity on hand after this movement.</summary>
    public decimal BalanceQuantity { get; private set; }

    /// <summary>The value on hand after this movement.</summary>
    public Money BalanceValue { get; private set; } = default!;

    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public Guid? DocumentId { get; private set; }
    public string? Description { get; private set; }

    private StockLedgerEntry() { }

    public StockLedgerEntry(
        Item item,
        Guid warehouseId,
        StockMovementType movementType,
        decimal quantityChange,
        Money valueChange,
        Guid unitOfMeasureId,
        decimal balanceQuantity,
        Money balanceValue,
        DateTimeOffset movementDateTime)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(valueChange, nameof(valueChange));
        ArgumentNullException.ThrowIfNull(balanceValue, nameof(balanceValue));
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is empty.", nameof(warehouseId));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (quantityChange == 0m)
            throw new ArgumentException("A ledger entry must move a non zero quantity.", nameof(quantityChange));
        if (balanceQuantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(balanceQuantity), "The balance cannot go negative.");

        Item = item;
        ItemId = item.Id;
        WarehouseId = warehouseId;
        MovementType = movementType;
        QuantityChange = quantityChange;
        ValueChange = valueChange;
        UnitOfMeasureId = unitOfMeasureId;
        BalanceQuantity = balanceQuantity;
        BalanceValue = balanceValue;
        MovementDateTime = movementDateTime;
        UnitCost = quantityChange == 0m
            ? Money.Zero(valueChange.CurrencyCode)
            : Money.Create(Math.Abs(valueChange.Amount / quantityChange), valueChange.CurrencyCode);

        Raise(new StockMoved(ItemId, WarehouseId, QuantityChange, BalanceQuantity, MovementType.ToString()));
    }

    public void SetTracking(Guid? itemBatchId, Guid? serialNumberId)
    {
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
    }

    public void ScopeToStorageLocation(Guid storageLocationId) =>
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;

    public void SetReference(string referenceType, string referenceId, string? referenceNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
        ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? null : referenceNumber.Trim();
    }

    public void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public bool IsReceipt => QuantityChange > 0m;
    public bool IsIssue => QuantityChange < 0m;
}
