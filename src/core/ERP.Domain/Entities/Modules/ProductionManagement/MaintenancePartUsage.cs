using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// A spare part consumed by a maintenance job, which is also a stock issue.
/// </summary>
public sealed class MaintenancePartUsage : BaseEntity
{
    public MaintenanceWorkOrder WorkOrder { get; private set; } = default!;
    public Guid WorkOrderId { get; private set; }

    public Item Part { get; private set; } = default!;
    public Guid PartId { get; private set; }

    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public Money UnitCost { get; private set; } = default!;
    public Money TotalCost => UnitCost.Multiply(Quantity);

    public Guid? WarehouseId { get; private set; }
    public Guid? ItemBatchId { get; private set; }
    public string? Description { get; private set; }

    private MaintenancePartUsage() { }

    public MaintenancePartUsage(MaintenanceWorkOrder workOrder, Item part, decimal quantity, Guid unitOfMeasureId, Money unitCost)
    {
        ArgumentNullException.ThrowIfNull(workOrder, nameof(workOrder));
        ArgumentNullException.ThrowIfNull(part, nameof(part));
        ArgumentNullException.ThrowIfNull(unitCost, nameof(unitCost));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        WorkOrder = workOrder;
        WorkOrderId = workOrder.Id;
        Part = part;
        PartId = part.Id;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        UnitCost = unitCost;
    }

    public void SetSource(Guid? warehouseId, Guid? itemBatchId)
    {
        WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
