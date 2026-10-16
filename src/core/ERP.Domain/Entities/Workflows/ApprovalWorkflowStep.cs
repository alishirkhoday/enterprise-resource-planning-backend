namespace ERP.Domain.Entities.Workflows;

/// <summary>
/// One rung of an <see cref="ApprovalWorkflow"/>. Who may approve is expressed indirectly through
/// <see cref="ApproverType"/> so the chain survives staff changes.
/// </summary>
public sealed class ApprovalWorkflowStep : BaseEntity
{
    public ApprovalWorkflow Workflow { get; private set; } = default!;
    public Guid WorkflowId { get; private set; }

    public int Order { get; private set; }
    public string Title { get; private set; } = default!;

    public ApproverType ApproverType { get; private set; }

    /// <summary>Identifier of the role, position or user that may act, depending on <see cref="ApproverType"/>.</summary>
    public Guid? ApproverReferenceId { get; private set; }

    /// <summary>How many distinct approvals this step needs before it is satisfied.</summary>
    public int RequiredApprovals { get; private set; }

    /// <summary>When set, the step is skipped for amounts below this threshold.</summary>
    public decimal? MinimumAmount { get; private set; }

    public bool CanReject { get; private set; }

    private ApprovalWorkflowStep() { }

    public ApprovalWorkflowStep(ApprovalWorkflow workflow, int order, string title, ApproverType approverType, Guid? approverReferenceId = null, int requiredApprovals = 1)
    {
        ArgumentNullException.ThrowIfNull(workflow, nameof(workflow));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order, nameof(order));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requiredApprovals, nameof(requiredApprovals));

        if (approverType is ApproverType.Role or ApproverType.Position or ApproverType.SpecificUser && approverReferenceId is null)
            throw new ArgumentNullException(nameof(approverReferenceId), $"An approver reference is required for {approverType}.");

        Workflow = workflow;
        WorkflowId = workflow.Id;
        Order = order;
        Title = title.Trim();
        ApproverType = approverType;
        ApproverReferenceId = approverReferenceId;
        RequiredApprovals = requiredApprovals;
        CanReject = true;
    }

    public void SetMinimumAmount(decimal? minimumAmount)
    {
        if (minimumAmount.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(minimumAmount.Value, nameof(minimumAmount));
        MinimumAmount = minimumAmount;
    }

    public void AllowRejection() => CanReject = true;
    public void ForbidRejection() => CanReject = false;

    /// <summary>Whether this step applies to a document of the given value.</summary>
    public bool AppliesTo(decimal amount) => MinimumAmount is null || amount >= MinimumAmount.Value;
}
