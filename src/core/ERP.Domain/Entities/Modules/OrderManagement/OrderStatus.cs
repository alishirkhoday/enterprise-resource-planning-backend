namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Where a customer order has got to, from basket to delivered.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Completed = 3,
    Refunded = 4
}
