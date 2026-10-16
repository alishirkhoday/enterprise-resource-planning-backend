namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Why money went back. Reported on, because a rising count for one reason is usually a
/// problem somewhere else in the business.
/// </summary>
public enum RefundReason
{
    OrderCancelled = 0,
    ItemReturned = 1,
    ItemUnavailable = 2,
    DuplicatePayment = 3,
    Overpayment = 4,
    DeliveryFailed = 5,
    QualityComplaint = 6,
    Other = 99
}
