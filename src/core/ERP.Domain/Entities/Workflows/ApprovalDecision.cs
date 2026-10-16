namespace ERP.Domain.Entities.Workflows;

/// <summary>
/// What one approver said at their step.
/// </summary>
public enum ApprovalDecision
{
    Approved = 0,
    Rejected = 1,
    ReturnedForRevision = 2,
    Cancelled = 3
}
