using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// A part consumed on a visit. It is both a stock movement and, sometimes, a charge - a part fitted
/// under warranty still leaves the van, it just does not reach the invoice.
/// </summary>
public sealed class ServiceOrderPart : BaseEntity
{
    public ServiceOrder ServiceOrder { get; private set; } = default!;
    public Guid ServiceOrderId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }

    public Money UnitPrice { get; private set; } = default!;

    /// <summary>What it cost this organization, kept apart from the price so the margin is visible.</summary>
    public Money? UnitCost { get; private set; }

    public Guid? ItemBatchId { get; private set; }
    public Guid? SerialNumberId { get; private set; }

    /// <summary>
    /// False when the part is covered - by warranty, by a comprehensive contract, or by goodwill.
    /// The part is still issued from stock; it simply does not reach the customer's bill.
    /// </summary>
    public bool IsChargeable { get; private set; }

    /// <summary>The part that came off, when the old one is being returned for inspection.</summary>
    public bool IsReturnedForInspection { get; private set; }

    public string? Description { get; private set; }

    private ServiceOrderPart() { }

    public ServiceOrderPart(ServiceOrder serviceOrder, Item item, short lineNumber, decimal quantity, Money unitPrice, Guid unitOfMeasureId)
    {
        ArgumentNullException.ThrowIfNull(serviceOrder, nameof(serviceOrder));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(unitPrice, nameof(unitPrice));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (unitPrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "A unit price cannot be negative.");

        ServiceOrder = serviceOrder;
        ServiceOrderId = serviceOrder.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitOfMeasureId = unitOfMeasureId;
        IsChargeable = serviceOrder.IsChargeable;
    }

    public void SetTracking(Guid? itemBatchId, Guid? serialNumberId)
    {
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
    }

    public void SetUnitCost(Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        UnitCost = unitCost;
    }

    /// <summary>Covers the part, so it is issued from stock but not billed.</summary>
    public void CoverUnderWarranty() => IsChargeable = false;

    public void MakeChargeable()
    {
        if (!ServiceOrder.IsChargeable)
            throw new InvalidOperationException($"A part on a {ServiceOrder.Type} service order cannot be charged to the customer.");
        IsChargeable = true;
    }

    public void MarkAsReturnedForInspection() => IsReturnedForInspection = true;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public Money LineTotal => IsChargeable ? UnitPrice.Multiply(Quantity) : Money.Zero(UnitPrice.CurrencyCode);

    public Money? LineCost => UnitCost?.Multiply(Quantity);
}
