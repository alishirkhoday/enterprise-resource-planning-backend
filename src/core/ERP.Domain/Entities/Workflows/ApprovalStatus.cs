namespace ERP.Domain.Entities.Workflows;

/// <summary>
/// Where a whole approval chain has got to, as against what one approver decided.
/// </summary>
public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    ReturnedForRevision = 3,
    Cancelled = 4
}
