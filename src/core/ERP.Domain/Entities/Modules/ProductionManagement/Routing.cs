using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// The ordered set of operations that turns materials into one item. Together with the bill of
/// materials it is what a production order is expanded from.
/// </summary>
public sealed partial class Routing : AggregateRoot
{
    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int Version { get; private set; }
    public RoutingStatus Status { get; private set; }

    public DateRange ValidityPeriod { get; private set; } = default!;

    /// <summary>The quantity the operation times are quoted for.</summary>
    public decimal BaseQuantity { get; private set; }

    public bool IsDefault { get; private set; }
    public string? Description { get; private set; }

    private readonly List<RoutingOperation> _operations = [];
    public IReadOnlyList<RoutingOperation> Operations => _operations.AsReadOnly();

    private Routing() { }

    public Routing(Item item, string code, string name, DateRange validityPeriod, decimal baseQuantity = 1m)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(validityPeriod, nameof(validityPeriod));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseQuantity, nameof(baseQuantity));
        if (!item.IsManufactured)
            throw new InvalidOperationException($"Item {item.Name} is not marked as manufactured.");

        Item = item;
        ItemId = item.Id;
        Code = code.Trim();
        Name = name.Trim();
        ValidityPeriod = validityPeriod;
        BaseQuantity = baseQuantity;
        Version = 1;
        Status = RoutingStatus.Draft;
    }

    public void AddOperation(RoutingOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        EnsureCanAddChild(operation, operation.RoutingId, _operations);

        EnsureDraft();
        if (_operations.Any(o => o.SequenceNumber == operation.SequenceNumber))
            throw new InvalidOperationException($"Operation {operation.SequenceNumber} already exists on this routing.");
        _operations.Add(operation);
    }

    public void RemoveOperation(int sequenceNumber)
    {
        EnsureDraft();
        var operation = _operations.FirstOrDefault(o => o.SequenceNumber == sequenceNumber);
        if (operation is not null)
            _operations.Remove(operation);
    }

    public void Approve()
    {
        EnsureDraft();
        if (_operations.Count == 0)
            throw new InvalidOperationException("A routing cannot be approved without operations.");
        Status = RoutingStatus.Approved;
    }

    /// <summary>Opens a new version, leaving the approved one untouched for orders already using it.</summary>
    public void Revise()
    {
        if (Status != RoutingStatus.Approved)
            throw new InvalidOperationException("Only an approved routing can be revised.");
        Version++;
        Status = RoutingStatus.Draft;
    }

    public void Obsolete()
    {
        Status = RoutingStatus.Obsolete;
        IsDefault = false;
    }

    public void MarkAsDefault()
    {
        if (Status != RoutingStatus.Approved)
            throw new InvalidOperationException("Only an approved routing can be made the default.");
        IsDefault = true;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Total run time for a quantity, scaled from the base quantity.</summary>
    public decimal TotalRunHoursFor(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        var factor = quantity / BaseQuantity;
        return _operations.Sum(o => o.SetupHours + o.RunHours * factor);
    }

    public bool IsEffectiveOn(DateOnly date) => Status == RoutingStatus.Approved && ValidityPeriod.Contains(date);

    private void EnsureDraft()
    {
        if (Status != RoutingStatus.Draft)
            throw new InvalidOperationException($"The routing is {Status} and can no longer be edited; revise it instead.");
    }
}
