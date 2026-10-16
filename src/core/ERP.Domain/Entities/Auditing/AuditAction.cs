namespace ERP.Domain.Entities.Auditing;

/// <summary>
/// What was done to a row, as the audit trail records it.
/// </summary>
public enum AuditAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Restored = 3,
    Approved = 4,
    Rejected = 5,
    Confirmed = 6,
    Cancelled = 7,
    Exported = 8,
    Viewed = 9
}
