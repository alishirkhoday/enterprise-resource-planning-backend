namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One line of an internal request to buy something.
/// </summary>
public sealed class PurchaseRequisitionItem : BaseEntity
{
    public PurchaseRequisition Requisition { get; private set; } = default!;
    public Guid RequisitionId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>How much of the requested quantity has been put on a purchase order.</summary>
    public decimal OrderedQuantity { get; private set; }

    public Money? EstimatedUnitPrice { get; private set; }
    public Guid? SuggestedSupplierId { get; private set; }
    public DateOnly? RequiredByDate { get; private set; }
    public string? Specification { get; private set; }

    public decimal RemainingQuantity => Quantity - OrderedQuantity;
    public bool IsFullyOrdered => OrderedQuantity >= Quantity;

    private PurchaseRequisitionItem() { }

    public PurchaseRequisitionItem(PurchaseRequisition requisition, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId)
    {
        ArgumentNullException.ThrowIfNull(requisition, nameof(requisition));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        Requisition = requisition;
        RequisitionId = requisition.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
    }

    public void SetEstimatedUnitPrice(Money estimatedUnitPrice)
    {
        ArgumentNullException.ThrowIfNull(estimatedUnitPrice, nameof(estimatedUnitPrice));
        if (estimatedUnitPrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(estimatedUnitPrice), "An estimated price cannot be negative.");
        EstimatedUnitPrice = estimatedUnitPrice;
    }

    public void SuggestSupplier(Guid supplierId) => SuggestedSupplierId = supplierId == Guid.Empty ? null : supplierId;

    public void SetRequiredByDate(DateOnly requiredByDate) => RequiredByDate = requiredByDate;

    public void SetSpecification(string? specification) =>
        Specification = string.IsNullOrWhiteSpace(specification) ? null : specification.Trim();

    public void RecordOrdered(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (OrderedQuantity + quantity > Quantity)
            throw new InvalidOperationException($"Ordering {quantity} would exceed the requested {Quantity}.");
        OrderedQuantity += quantity;
    }
}
