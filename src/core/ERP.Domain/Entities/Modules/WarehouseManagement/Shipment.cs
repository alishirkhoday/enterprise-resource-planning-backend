using ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;

namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// Goods leaving the warehouse towards a customer, with the paperwork and tracking that go with them.
/// </summary>
public sealed partial class Shipment : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;

    /// <summary>What is being shipped, for example SaleOrder together with its identifier.</summary>
    public string ReferenceType { get; private set; } = default!;
    public string ReferenceId { get; private set; } = default!;

    public Guid WarehouseId { get; private set; }
    public ShipmentStatus Status { get; private set; }

    public DateOnly ShipmentDate { get; private set; }
    public DateOnly? EstimatedDeliveryDate { get; private set; }
    public DateOnly? DeliveredOn { get; private set; }

    public ShipToAddress ShipToAddress { get; private set; } = default!;

    public Guid? CarrierId { get; private set; }
    public string? CarrierName { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? VehicleNumber { get; private set; }
    public string? DriverName { get; private set; }

    /// <summary>The freight charged for the shipment.</summary>
    public Money? FreightCost { get; private set; }

    public decimal? TotalWeightInKilograms { get; private set; }
    public int? PackageCount { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? Description { get; private set; }

    private readonly List<ShipmentItem> _items = [];
    public IReadOnlyList<ShipmentItem> Items => _items.AsReadOnly();

    private Shipment() { }

    public Shipment(string number, string referenceType, string referenceId, Guid warehouseId, DateOnly shipmentDate, ShipToAddress shipToAddress)
    {
        ArgumentNullException.ThrowIfNull(shipToAddress, nameof(shipToAddress));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is empty.", nameof(warehouseId));

        Number = number.Trim();
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
        WarehouseId = warehouseId;
        ShipmentDate = shipmentDate;
        ShipToAddress = shipToAddress;
        Status = ShipmentStatus.Draft;
    }

    public void SetCarrier(Guid? carrierId, string? carrierName, string? trackingNumber)
    {
        CarrierId = carrierId == Guid.Empty ? null : carrierId;
        CarrierName = string.IsNullOrWhiteSpace(carrierName) ? null : carrierName.Trim();
        TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
    }

    public void SetTransport(string? vehicleNumber, string? driverName)
    {
        VehicleNumber = string.IsNullOrWhiteSpace(vehicleNumber) ? null : vehicleNumber.Trim();
        DriverName = string.IsNullOrWhiteSpace(driverName) ? null : driverName.Trim();
    }

    public void SetFreightCost(Money freightCost)
    {
        ArgumentNullException.ThrowIfNull(freightCost, nameof(freightCost));
        if (freightCost.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(freightCost), "Freight cannot be negative.");
        FreightCost = freightCost;
    }

    public void SetDimensions(decimal? totalWeightInKilograms, int? packageCount)
    {
        if (totalWeightInKilograms.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(totalWeightInKilograms.Value, nameof(totalWeightInKilograms));
        if (packageCount.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(packageCount.Value, nameof(packageCount));
        TotalWeightInKilograms = totalWeightInKilograms;
        PackageCount = packageCount;
    }

    public void SetEstimatedDeliveryDate(DateOnly estimatedDeliveryDate)
    {
        if (estimatedDeliveryDate < ShipmentDate)
            throw new ArgumentOutOfRangeException(nameof(estimatedDeliveryDate), "The delivery date cannot precede the shipment date.");
        EstimatedDeliveryDate = estimatedDeliveryDate;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(ShipmentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.ShipmentId, _items);

        EnsureDraft();
        _items.Add(item);
    }

    public void Pack()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A shipment cannot be packed without lines.");
        Status = ShipmentStatus.Packed;
    }

    public void Dispatch()
    {
        if (Status != ShipmentStatus.Packed)
            throw new InvalidOperationException($"Only a packed shipment can be dispatched; this one is {Status}.");
        Status = ShipmentStatus.InTransit;
    }

    public void MarkAsDelivered(DateOnly deliveredOn)
    {
        if (Status != ShipmentStatus.InTransit)
            throw new InvalidOperationException($"Only a shipment in transit can be delivered; this one is {Status}.");
        DeliveredOn = deliveredOn;
        Status = ShipmentStatus.Delivered;
    }

    public void MarkAsFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != ShipmentStatus.InTransit)
            throw new InvalidOperationException("Only a shipment in transit can fail delivery.");
        Status = ShipmentStatus.DeliveryFailed;
        Description = reason.Trim();
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == ShipmentStatus.Delivered)
            throw new InvalidOperationException("A delivered shipment cannot be cancelled.");
        Status = ShipmentStatus.Cancelled;
        Description = reason.Trim();
    }

    private void EnsureDraft()
    {
        if (Status != ShipmentStatus.Draft)
            throw new InvalidOperationException($"The shipment is {Status} and can no longer be edited.");
    }
}
