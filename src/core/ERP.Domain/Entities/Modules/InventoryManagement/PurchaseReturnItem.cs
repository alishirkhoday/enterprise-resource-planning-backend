namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One line going back to the supplier.
/// </summary>
public sealed class PurchaseReturnItem : BaseEntity
{
    public PurchaseReturn Return { get; private set; } = default!;
    public Guid ReturnId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitPrice { get; private set; } = default!;

    public Guid? ItemBatchId { get; private set; }
    public Guid? SerialNumberId { get; private set; }
    public string? Description { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private PurchaseReturnItem() { }

    public PurchaseReturnItem(PurchaseReturn purchaseReturn, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId, Money unitPrice)
    {
        ArgumentNullException.ThrowIfNull(purchaseReturn, nameof(purchaseReturn));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(unitPrice, nameof(unitPrice));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        Return = purchaseReturn;
        ReturnId = purchaseReturn.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitPrice = unitPrice;
    }

    internal void SetTracking(Guid? itemBatchId, Guid? serialNumberId)
    {
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
