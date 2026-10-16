using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Workflows;
using ERP.Domain.Repositories.Workflows;

namespace ERP.Infrastructure.MainDatabase.Repositories.Workflows;

public class ApprovalRequestRepository(IMainDbContext context) : Repository<ApprovalRequest>(context), IApprovalRequestRepository
{
    /// <summary>
    /// Tracked and fully loaded: the caller is about to record a decision, and whether the request
    /// may advance depends on the steps and on what has already been approved.
    /// </summary>
    public async Task<ApprovalRequest?> GetWithWorkflowAndActionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(r => r.Workflow).ThenInclude(w => w.Steps)
            .Include(r => r.Actions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<ApprovalRequest?> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(r => r.Workflow).ThenInclude(w => w.Steps)
            .Include(r => r.Actions)
            .Where(r => r.ReferenceType == referenceType && r.ReferenceId == referenceId)
            .OrderByDescending(r => r.RequestedOn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Matching is done against the step the request currently sits on. DirectManager and
    /// DepartmentHead are not resolved here: they depend on the requester's place in the
    /// organization chart, which this repository has no business reading.
    /// </summary>
    public async Task<IReadOnlyList<ApprovalRequest>> GetPendingForApproverAsync(
        Guid userId,
        IEnumerable<Guid> roleIds,
        Guid? positionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds, nameof(roleIds));
        var roles = roleIds.Distinct().ToArray();

        return await Query
            .Include(r => r.Workflow)
            .Where(r => r.Status == ApprovalStatus.Pending
                        && r.Workflow.Steps.Any(s =>
                            s.Order == r.CurrentStepOrder
                            && ((s.ApproverType == ApproverType.SpecificUser && s.ApproverReferenceId == userId)
                                || (s.ApproverType == ApproverType.Role && s.ApproverReferenceId != null && roles.Contains(s.ApproverReferenceId.Value))
                                || (s.ApproverType == ApproverType.Position && positionId != null && s.ApproverReferenceId == positionId)))
                        && !r.Actions.Any(a => a.StepOrder == r.CurrentStepOrder
                                               && a.ApproverUserId == userId
                                               && a.Decision == ApprovalDecision.Approved))
            .OrderBy(r => r.RequestedOn)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// A branch may override the company-wide chain, so a branch-specific workflow wins over the
    /// unscoped one for the same entity type.
    /// </summary>
    public async Task<ApprovalWorkflow?> GetActiveWorkflowForAsync(string entityType, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflows
            .Include(w => w.Steps)
            .Where(w => w.EntityType == entityType
                        && w.IsActive
                        && (w.BranchId == null || w.BranchId == branchId))
            .OrderByDescending(w => w.BranchId != null)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
