namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Material moving from the warehouse into a production order. This is the link that was missing
/// between production and inventory: without it, making something consumes nothing.
/// </summary>
public sealed partial class MaterialIssue : AggregateRoot
{
    public string Number { get; private set; } = default!;

    public ProductionOrder ProductionOrder { get; private set; } = default!;
    public Guid ProductionOrderId { get; private set; }

    public Guid WarehouseId { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public MaterialIssueStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    /// <summary>The routing step the material is being issued for.</summary>
    public int? OperationSequenceNumber { get; private set; }

    public Guid? IssuedByHumanId { get; private set; }
    public Guid? WarehouseTransactionId { get; private set; }
    public string? Description { get; private set; }

    private readonly List<MaterialIssueItem> _items = [];
    public IReadOnlyList<MaterialIssueItem> Items => _items.AsReadOnly();

    private MaterialIssue() { }

    public MaterialIssue(string number, ProductionOrder productionOrder, Guid warehouseId, DateOnly issueDate, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(productionOrder, nameof(productionOrder));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is empty.", nameof(warehouseId));
        if (productionOrder.Status is not (ProductionOrderStatus.Released or ProductionOrderStatus.InProgress))
            throw new InvalidOperationException($"Material cannot be issued to an order in state {productionOrder.Status}.");

        Number = number.Trim();
        ProductionOrder = productionOrder;
        ProductionOrderId = productionOrder.Id;
        WarehouseId = warehouseId;
        IssueDate = issueDate;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Status = MaterialIssueStatus.Draft;
    }

    public void SetOperation(int operationSequenceNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(operationSequenceNumber, nameof(operationSequenceNumber));
        OperationSequenceNumber = operationSequenceNumber;
    }

    public void SetIssuer(Guid humanId) => IssuedByHumanId = humanId == Guid.Empty ? null : humanId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(MaterialIssueItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.IssueId, _items);

        EnsureDraft();
        if (item.UnitCost.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Issue lines must be valued in {CurrencyCode}.");
        _items.Add(item);
    }

    public Money TotalCost() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.TotalCost).Aggregate((accumulated, next) => accumulated.Add(next));

    /// <summary>
    /// Confirms the issue and charges its value to the order. The caller is expected to write the
    /// matching stock ledger entries in the same transaction.
    /// </summary>
    public void Confirm(Guid? warehouseTransactionId = null)
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A material issue cannot be confirmed without lines.");

        var zero = Money.Zero(CurrencyCode);
        ProductionOrder.AccumulateCost(TotalCost(), zero, zero);
        WarehouseTransactionId = warehouseTransactionId == Guid.Empty ? null : warehouseTransactionId;
        Status = MaterialIssueStatus.Confirmed;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == MaterialIssueStatus.Confirmed)
            throw new InvalidOperationException("A confirmed issue must be reversed by a return, not cancelled.");
        Status = MaterialIssueStatus.Cancelled;
        Description = reason.Trim();
    }

    private void EnsureDraft()
    {
        if (Status != MaterialIssueStatus.Draft)
            throw new InvalidOperationException($"The issue is {Status} and can no longer be edited.");
    }
}
