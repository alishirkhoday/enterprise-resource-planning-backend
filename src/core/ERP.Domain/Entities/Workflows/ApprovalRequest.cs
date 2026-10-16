namespace ERP.Domain.Entities.Workflows;

/// <summary>
/// One document travelling through an <see cref="ApprovalWorkflow"/>, together with every decision
/// taken on it. The document itself only needs to read <see cref="Status"/>.
/// </summary>
public sealed class ApprovalRequest : BaseEntity
{
    public ApprovalWorkflow Workflow { get; private set; } = default!;
    public Guid WorkflowId { get; private set; }

    public string ReferenceType { get; private set; } = default!;
    public string ReferenceId { get; private set; } = default!;

    public Guid RequestedByUserId { get; private set; }
    public DateTimeOffset RequestedOn { get; private set; }

    public ApprovalStatus Status { get; private set; }
    public int CurrentStepOrder { get; private set; }
    public DateTimeOffset? CompletedOn { get; private set; }

    /// <summary>The value the document carries, used to skip steps below their threshold.</summary>
    public decimal? Amount { get; private set; }

    private readonly List<ApprovalAction> _actions = [];
    public IReadOnlyList<ApprovalAction> Actions => _actions;

    private ApprovalRequest() { }

    public ApprovalRequest(ApprovalWorkflow workflow, string referenceType, string referenceId, Guid requestedByUserId, decimal? amount = null)
    {
        ArgumentNullException.ThrowIfNull(workflow, nameof(workflow));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        if (requestedByUserId == Guid.Empty)
            throw new ArgumentException("RequestedByUserId is empty.", nameof(requestedByUserId));

        var firstStep = workflow.FirstStep()
            ?? throw new InvalidOperationException($"Workflow {workflow.Key} has no steps.");

        Workflow = workflow;
        WorkflowId = workflow.Id;
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
        RequestedByUserId = requestedByUserId;
        RequestedOn = DateTimeOffset.UtcNow;
        Amount = amount;
        Status = ApprovalStatus.Pending;
        CurrentStepOrder = firstStep.Order;
        Raise(new ApprovalRequested(Id, ReferenceType, ReferenceId, CurrentStepOrder));
    }

    /// <summary>
    /// Records one approval. When the current step has collected enough approvals the request
    /// advances, and when no step remains it is approved outright.
    /// </summary>
    public void Approve(Guid approverUserId, string? comment = null)
    {
        EnsurePending();

        var step = CurrentStep()
            ?? throw new InvalidOperationException("The current step no longer exists in the workflow.");

        if (_actions.Any(a => a.StepOrder == CurrentStepOrder && a.ApproverUserId == approverUserId && a.Decision == ApprovalDecision.Approved))
            throw new InvalidOperationException("This approver has already approved the current step.");

        _actions.Add(new ApprovalAction(this, CurrentStepOrder, approverUserId, ApprovalDecision.Approved, comment));

        var approvalsOnStep = _actions.Count(a => a.StepOrder == CurrentStepOrder && a.Decision == ApprovalDecision.Approved);
        if (approvalsOnStep < step.RequiredApprovals)
            return;

        var nextStep = NextApplicableStep(step.Order);
        if (nextStep is null)
        {
            Status = ApprovalStatus.Approved;
            CompletedOn = DateTimeOffset.UtcNow;
            Raise(new ApprovalGranted(Id, ReferenceType, ReferenceId));
            return;
        }

        CurrentStepOrder = nextStep.Order;
    }

    public void Reject(Guid approverUserId, string reason)
    {
        EnsurePending();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        var step = CurrentStep()
            ?? throw new InvalidOperationException("The current step no longer exists in the workflow.");

        if (!step.CanReject)
            throw new InvalidOperationException($"Step {step.Title} does not allow rejection.");

        _actions.Add(new ApprovalAction(this, CurrentStepOrder, approverUserId, ApprovalDecision.Rejected, reason));
        Status = ApprovalStatus.Rejected;
        CompletedOn = DateTimeOffset.UtcNow;
        Raise(new ApprovalRejected(Id, ReferenceType, ReferenceId, reason.Trim()));
    }

    /// <summary>Sends the request back to an earlier step for correction without rejecting it.</summary>
    public void ReturnForRevision(Guid approverUserId, string reason)
    {
        EnsurePending();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        _actions.Add(new ApprovalAction(this, CurrentStepOrder, approverUserId, ApprovalDecision.ReturnedForRevision, reason));
        Status = ApprovalStatus.ReturnedForRevision;
    }

    /// <summary>Restarts the chain from the first step after the document has been corrected.</summary>
    public void Resubmit()
    {
        if (Status != ApprovalStatus.ReturnedForRevision)
            throw new InvalidOperationException("Only a request returned for revision can be resubmitted.");

        var firstStep = Workflow.FirstStep()
            ?? throw new InvalidOperationException("The workflow has no steps.");

        CurrentStepOrder = firstStep.Order;
        Status = ApprovalStatus.Pending;
    }

    public void Cancel(Guid userId, string? reason = null)
    {
        if (Status is ApprovalStatus.Approved or ApprovalStatus.Rejected)
            throw new InvalidOperationException("A completed request cannot be cancelled.");
        _actions.Add(new ApprovalAction(this, CurrentStepOrder, userId, ApprovalDecision.Cancelled, reason));
        Status = ApprovalStatus.Cancelled;
        CompletedOn = DateTimeOffset.UtcNow;
    }

    public ApprovalWorkflowStep? CurrentStep() => Workflow.Steps.FirstOrDefault(s => s.Order == CurrentStepOrder);

    private ApprovalWorkflowStep? NextApplicableStep(int afterOrder)
    {
        var candidate = Workflow.StepAfter(afterOrder);
        while (candidate is not null && Amount.HasValue && !candidate.AppliesTo(Amount.Value))
        {
            candidate = Workflow.StepAfter(candidate.Order);
        }
        return candidate;
    }

    private void EnsurePending()
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"The request is {Status} and no longer accepts decisions.");
    }
}
