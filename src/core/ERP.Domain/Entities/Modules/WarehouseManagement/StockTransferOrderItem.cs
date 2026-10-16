using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// One line of a transfer: what was asked for, what left, and what arrived.
/// 
/// All three are kept because they routinely differ, and the gap between dispatched and
/// received is stock that left one building and never reached another.
/// </summary>
public sealed class StockTransferOrderItem : BaseEntity
{
    public StockTransferOrder Transfer { get; private set; } = default!;
    public Guid TransferId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal RequestedQuantity { get; private set; }
    public decimal DispatchedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitCost { get; private set; } = default!;

    public Guid? ItemBatchId { get; private set; }
    public Guid? SourceStorageLocationId { get; private set; }
    public Guid? DestinationStorageLocationId { get; private set; }
    public string? DiscrepancyReason { get; private set; }

    /// <summary>What left the source but never arrived.</summary>
    public decimal ShortageQuantity => DispatchedQuantity - ReceivedQuantity;
    public bool HasShortage => DispatchedQuantity > 0m && ReceivedQuantity < DispatchedQuantity;
    public Money ShortageValue => UnitCost.Multiply(ShortageQuantity);

    private StockTransferOrderItem() { }

    public StockTransferOrderItem(StockTransferOrder transfer, Item item, short lineNumber, decimal requestedQuantity, Guid unitOfMeasureId, Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(transfer, nameof(transfer));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requestedQuantity, nameof(requestedQuantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        Transfer = transfer;
        TransferId = transfer.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        RequestedQuantity = requestedQuantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitCost = unitCost;
    }

    public void SetBatch(Guid itemBatchId) => ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;

    public void SetStorageLocations(Guid? sourceStorageLocationId, Guid? destinationStorageLocationId)
    {
        SourceStorageLocationId = sourceStorageLocationId == Guid.Empty ? null : sourceStorageLocationId;
        DestinationStorageLocationId = destinationStorageLocationId == Guid.Empty ? null : destinationStorageLocationId;
    }

    /// <summary>
    /// Restates what the goods are worth, at the moment they actually leave.
    /// <para>
    /// The cost put on the line when the transfer was raised is a quote. Days may pass before the
    /// pallet moves, and the source's average cost moves with every receipt in between. Recording
    /// what left is what lets the destination book in the same value the source booked out, which
    /// is the whole point: a transfer must not change what the goods are worth.
    /// </para>
    /// </summary>
    public void SetUnitCost(Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        if (unitCost.CurrencyCode != UnitCost.CurrencyCode)
            throw new ArgumentException($"The cost must be expressed in {UnitCost.CurrencyCode}.", nameof(unitCost));
        if (unitCost.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitCost), "A cost cannot be negative.");
        if (ReceivedQuantity > 0m)
            throw new InvalidOperationException("The cost cannot be restated once the goods have been received.");
        UnitCost = unitCost;
    }

    public void Dispatch(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > RequestedQuantity)
            throw new InvalidOperationException($"Dispatching {quantity} would exceed the requested {RequestedQuantity}.");
        DispatchedQuantity = quantity;
    }

    public void Receive(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity, nameof(quantity));
        if (quantity > DispatchedQuantity)
            throw new InvalidOperationException($"Receiving {quantity} would exceed the dispatched {DispatchedQuantity}.");
        ReceivedQuantity = quantity;
    }

    public void ExplainDiscrepancy(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        DiscrepancyReason = reason.Trim();
    }
}
