namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Where one payment attempt got to. Failed attempts are kept: a customer whose card keeps
/// being declined is a different conversation from one who never tried.
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    PartiallyRefunded = 3,
    Refunded = 4,
    Cancelled = 5
}
