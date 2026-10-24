using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// An instruction to make a quantity of an item. It ties the bill of materials and the routing
/// together, and collects the material, labour and overhead that end up in the unit cost.
/// </summary>
public sealed partial class ProductionOrder : AggregateRoot, IBranchScoped
{
    public string Code { get; private set; } = default!;

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public decimal Quantity { get; private set; }
    public Guid? UnitOfMeasureId { get; private set; }

    public ProductionOrderStatus Status { get; private set; }
    public ProductionOrderPriority Priority { get; private set; }

    public DateTimeOffset CreatedDate { get; private set; } = DateTimeOffset.UtcNow;
    public DateOnly? PlannedStartDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? StartedDate { get; private set; }
    public DateTimeOffset? CompletedDate { get; private set; }

    public Guid? BillOfMaterialsId { get; private set; }
    public Guid? RoutingId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>What triggered the order, for example a sale order or the planning run.</summary>
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }

    public decimal ProducedQuantity { get; private set; }
    public decimal ScrappedQuantity { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public Money MaterialCost { get; private set; } = default!;
    public Money LabourCost { get; private set; } = default!;
    public Money OverheadCost { get; private set; } = default!;

    public Money TotalCost => MaterialCost.Add(LabourCost).Add(OverheadCost);

    public Money UnitCost => ProducedQuantity == 0m
        ? Money.Zero(CurrencyCode)
        : TotalCost.Divide(ProducedQuantity);

    public string? CancellationReason { get; private set; }

    private readonly List<ProductionTask> _tasks = [];
    public IReadOnlyList<ProductionTask> Tasks => _tasks.AsReadOnly();

    private ProductionOrder() { }

    public ProductionOrder(string code, Item item, decimal quantity, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (!item.IsManufactured)
            throw new InvalidOperationException($"Item {item.Name} is not marked as manufactured.");

        Code = code.Trim();
        Item = item;
        ItemId = item.Id;
        Quantity = quantity;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        MaterialCost = Money.Zero(CurrencyCode);
        LabourCost = Money.Zero(CurrencyCode);
        OverheadCost = Money.Zero(CurrencyCode);
        Status = ProductionOrderStatus.Created;
        Priority = ProductionOrderPriority.Normal;
    }

    public void Plan(Guid billOfMaterialsId, Guid routingId, DateOnly plannedStartDate, DateOnly dueDate)
    {
        if (Status != ProductionOrderStatus.Created)
            throw new InvalidOperationException($"Only a created order can be planned; this one is {Status}.");
        if (dueDate < plannedStartDate)
            throw new ArgumentOutOfRangeException(nameof(dueDate), "The due date cannot precede the planned start.");

        BillOfMaterialsId = billOfMaterialsId == Guid.Empty ? null : billOfMaterialsId;
        RoutingId = routingId == Guid.Empty ? null : routingId;
        PlannedStartDate = plannedStartDate;
        DueDate = dueDate;
        Status = ProductionOrderStatus.Planned;
    }

    public void Release()
    {
        if (Status != ProductionOrderStatus.Planned)
            throw new InvalidOperationException($"Only a planned order can be released; this one is {Status}.");
        Status = ProductionOrderStatus.Released;
        Raise(new ProductionOrderReleased(Id, Code, ItemId, Quantity));
    }

    public void SetPriority(ProductionOrderPriority priority) => Priority = priority;

    public void SetSource(string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
    }

    public void SetTargets(Guid? warehouseId, Guid? costCenterId, Guid? unitOfMeasureId)
    {
        WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;
        CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;
        UnitOfMeasureId = unitOfMeasureId == Guid.Empty ? null : unitOfMeasureId;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddTask(ProductionTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        EnsureCanAddChild(task, task.OrderId, _tasks);

        if (Status is ProductionOrderStatus.Completed or ProductionOrderStatus.Cancelled)
            throw new InvalidOperationException($"Tasks cannot be added to a {Status} order.");
        _tasks.Add(task);
    }

    public void Start()
    {
        if (Status is not (ProductionOrderStatus.Released or ProductionOrderStatus.Planned))
            throw new InvalidOperationException($"An order in state {Status} cannot be started.");
        Status = ProductionOrderStatus.InProgress;
        StartedDate = DateTimeOffset.UtcNow;
    }

    /// <summary>Records output from the shop floor, keeping produced and scrapped apart.</summary>
    public void RecordOutput(decimal producedQuantity, decimal scrappedQuantity = 0m)
    {
        if (Status != ProductionOrderStatus.InProgress)
            throw new InvalidOperationException("Output can only be recorded while the order is in progress.");
        ArgumentOutOfRangeException.ThrowIfNegative(producedQuantity, nameof(producedQuantity));
        ArgumentOutOfRangeException.ThrowIfNegative(scrappedQuantity, nameof(scrappedQuantity));
        if (ProducedQuantity + producedQuantity > Quantity)
            throw new InvalidOperationException($"Producing {producedQuantity} would exceed the ordered {Quantity}.");

        ProducedQuantity += producedQuantity;
        ScrappedQuantity += scrappedQuantity;
    }

    public void AccumulateCost(Money material, Money labour, Money overhead)
    {
        ArgumentNullException.ThrowIfNull(material, nameof(material));
        ArgumentNullException.ThrowIfNull(labour, nameof(labour));
        ArgumentNullException.ThrowIfNull(overhead, nameof(overhead));
        var materialCost = MaterialCost.Add(material);
        var labourCost = LabourCost.Add(labour);
        var overheadCost = OverheadCost.Add(overhead);
        MaterialCost = materialCost;
        LabourCost = labourCost;
        OverheadCost = overheadCost;
    }

    public void Complete()
    {
        if (Status != ProductionOrderStatus.InProgress)
            throw new InvalidOperationException($"An order in state {Status} cannot be completed.");
        if (_tasks.Any(t => t.Status is not (ProductionTaskStatus.Completed or ProductionTaskStatus.Cancelled)))
            throw new InvalidOperationException("Every task must be finished before the order is completed.");
        Status = ProductionOrderStatus.Completed;
        CompletedDate = DateTimeOffset.UtcNow;
        Raise(new ProductionOrderCompleted(Id, Code, ProducedQuantity, ScrappedQuantity));
    }

    public void Hold(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != ProductionOrderStatus.InProgress)
            throw new InvalidOperationException("Only an order in progress can be put on hold.");
        Status = ProductionOrderStatus.OnHold;
        CancellationReason = reason.Trim();
    }

    public void Resume()
    {
        if (Status != ProductionOrderStatus.OnHold)
            throw new InvalidOperationException("Only an order on hold can be resumed.");
        Status = ProductionOrderStatus.InProgress;
        CancellationReason = null;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == ProductionOrderStatus.Completed)
            throw new InvalidOperationException("A completed order cannot be cancelled.");
        Status = ProductionOrderStatus.Cancelled;
        CancellationReason = reason.Trim();
    }

    public decimal RemainingQuantity => Quantity - ProducedQuantity;

    /// <summary>Good output as a share of everything that came off the line.</summary>
    public decimal? YieldPercentage =>
        ProducedQuantity + ScrappedQuantity == 0m
            ? null
            : ProducedQuantity / (ProducedQuantity + ScrappedQuantity) * 100m;

    public bool IsLate(DateOnly asOf) =>
        Status is not (ProductionOrderStatus.Completed or ProductionOrderStatus.Cancelled)
        && DueDate.HasValue
        && asOf > DueDate.Value;
}
