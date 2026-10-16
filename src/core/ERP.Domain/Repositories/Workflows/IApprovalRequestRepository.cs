using ERP.Domain.Entities.Workflows;

namespace ERP.Domain.Repositories.Workflows;
/// <summary>
/// Things waiting on somebody to approve them. Shared across modules, so a request points at
/// its subject by type and identifier rather than by a foreign key.
/// </summary>
public interface IApprovalRequestRepository : IRepository<ApprovalRequest>
{
    /// <summary>
    /// A request with its workflow and the decisions taken so far. Whether it may advance is a
    /// question about the whole chain, so the chain is loaded with it.
    /// </summary>
    Task<ApprovalRequest?> GetWithWorkflowAndActionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The approval a particular document is waiting on, if it is waiting on one.</summary>
    Task<ApprovalRequest?> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// What is sitting in someone's approval queue: pending requests whose current step names a
    /// role they hold or a position they occupy.
    /// </summary>
    Task<IReadOnlyList<ApprovalRequest>> GetPendingForApproverAsync(
        Guid userId,
        IEnumerable<Guid> roleIds,
        Guid? positionId = null,
        CancellationToken cancellationToken = default);

    Task<ApprovalWorkflow?> GetActiveWorkflowForAsync(string entityType, Guid? branchId = null, CancellationToken cancellationToken = default);
}
