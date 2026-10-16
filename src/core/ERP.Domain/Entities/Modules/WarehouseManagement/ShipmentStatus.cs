namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// Where a delivery to a customer has got to.
/// </summary>
public enum ShipmentStatus
{
    Draft = 0,
    Packed = 1,
    InTransit = 2,
    Delivered = 3,
    DeliveryFailed = 4,
    Cancelled = 5
}
