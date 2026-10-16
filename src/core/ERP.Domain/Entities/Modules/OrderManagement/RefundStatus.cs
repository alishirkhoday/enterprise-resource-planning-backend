namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Where a refund has got to on its way back to the customer.
/// </summary>
public enum RefundStatus
{
    Requested = 0,
    Approved = 1,
    Processed = 2,
    Rejected = 3
}
