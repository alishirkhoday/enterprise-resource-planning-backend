namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Where a request to send something back has got to - درخواست مرجوعی.
/// </summary>
public enum ReturnRequestStatus
{
    Requested = 0,
    Approved = 1,
    Received = 2,
    Completed = 3,
    Rejected = 4,
    Cancelled = 5
}
