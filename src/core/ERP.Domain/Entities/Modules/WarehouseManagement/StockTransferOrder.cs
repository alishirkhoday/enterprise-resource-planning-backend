namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// A move of stock between two warehouses. It is two movements, not one: goods leave the source
/// before they arrive at the destination, and the gap between is stock in transit.
/// </summary>
public sealed class StockTransferOrder : BaseEntity
{
    public string Number { get; private set; } = default!;

    public Warehouse SourceWarehouse { get; private set; } = default!;
    public Guid SourceWarehouseId { get; private set; }

    public Warehouse DestinationWarehouse { get; private set; } = default!;
    public Guid DestinationWarehouseId { get; private set; }

    public DateOnly RequestDate { get; private set; }
    public DateOnly? ExpectedArrivalDate { get; private set; }
    public DateOnly? DispatchedOn { get; private set; }
    public DateOnly? ReceivedOn { get; private set; }

    public StockTransferStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    public Guid? RequestedByHumanId { get; private set; }
    public string? VehicleNumber { get; private set; }
    public string? Description { get; private set; }

    private readonly List<StockTransferOrderItem> _items = [];
    public IReadOnlyList<StockTransferOrderItem> Items => _items;

    private StockTransferOrder() { }

    public StockTransferOrder(string number, Warehouse sourceWarehouse, Warehouse destinationWarehouse, DateOnly requestDate, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(sourceWarehouse, nameof(sourceWarehouse));
        ArgumentNullException.ThrowIfNull(destinationWarehouse, nameof(destinationWarehouse));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (sourceWarehouse.Id == destinationWarehouse.Id)
            throw new ArgumentException("The source and destination warehouses must differ.", nameof(destinationWarehouse));

        Number = number.Trim();
        SourceWarehouse = sourceWarehouse;
        SourceWarehouseId = sourceWarehouse.Id;
        DestinationWarehouse = destinationWarehouse;
        DestinationWarehouseId = destinationWarehouse.Id;
        RequestDate = requestDate;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Status = StockTransferStatus.Draft;
    }

    public void SetExpectedArrivalDate(DateOnly expectedArrivalDate)
    {
        if (expectedArrivalDate < RequestDate)
            throw new ArgumentOutOfRangeException(nameof(expectedArrivalDate), "The arrival date cannot precede the request date.");
        ExpectedArrivalDate = expectedArrivalDate;
    }

    public void SetRequester(Guid humanId) => RequestedByHumanId = humanId == Guid.Empty ? null : humanId;

    public void SetVehicleNumber(string? vehicleNumber) =>
        VehicleNumber = string.IsNullOrWhiteSpace(vehicleNumber) ? null : vehicleNumber.Trim();

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(StockTransferOrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Add(item);
    }

    public void Approve()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A transfer cannot be approved without lines.");
        Status = StockTransferStatus.Approved;
    }

    /// <summary>
    /// Records the goods leaving the source. Stock now belongs to neither warehouse until it
    /// is received.
    /// </summary>
    public void Dispatch(DateOnly dispatchedOn)
    {
        if (Status != StockTransferStatus.Approved)
            throw new InvalidOperationException($"Only an approved transfer can be dispatched; this one is {Status}.");
        DispatchedOn = dispatchedOn;
        Status = StockTransferStatus.InTransit;
    }

    public void Receive(DateOnly receivedOn)
    {
        if (Status != StockTransferStatus.InTransit)
            throw new InvalidOperationException($"Only a transfer in transit can be received; this one is {Status}.");
        // A line showing nothing received is either one nobody has counted yet or a pallet that
        // never arrived. The two are told apart by whether somebody explained it, which is what
        // lets a genuinely lost line close the transfer instead of holding it open forever.
        if (_items.Any(i => i.ReceivedQuantity == 0m && i.DiscrepancyReason is null))
            throw new InvalidOperationException("Every line must record what arrived, or explain why nothing did.");

        ReceivedOn = receivedOn;
        Status = _items.Any(i => i.HasShortage) ? StockTransferStatus.ReceivedWithDiscrepancy : StockTransferStatus.Received;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status is StockTransferStatus.InTransit or StockTransferStatus.Received)
            throw new InvalidOperationException($"A transfer in state {Status} cannot be cancelled.");
        Status = StockTransferStatus.Cancelled;
        Description = reason.Trim();
    }

    public bool HasDiscrepancy => _items.Any(i => i.HasShortage);

    private void EnsureDraft()
    {
        if (Status != StockTransferStatus.Draft)
            throw new InvalidOperationException($"The transfer is {Status} and can no longer be edited.");
    }
}
