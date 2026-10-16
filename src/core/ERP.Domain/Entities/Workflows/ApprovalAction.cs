namespace ERP.Domain.Entities.Workflows;

/// <summary>
/// One decision taken on an <see cref="ApprovalRequest"/>. Actions are append only.
/// </summary>
public sealed class ApprovalAction : BaseEntity
{
    public ApprovalRequest Request { get; private set; } = default!;
    public Guid RequestId { get; private set; }

    public int StepOrder { get; private set; }
    public Guid ApproverUserId { get; private set; }
    public ApprovalDecision Decision { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset ActedOn { get; private set; }

    private ApprovalAction() { }

    internal ApprovalAction(ApprovalRequest request, int stepOrder, Guid approverUserId, ApprovalDecision decision, string? comment)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (approverUserId == Guid.Empty)
            throw new ArgumentException("ApproverUserId is empty.", nameof(approverUserId));
        Request = request;
        RequestId = request.Id;
        StepOrder = stepOrder;
        ApproverUserId = approverUserId;
        Decision = decision;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        ActedOn = DateTimeOffset.UtcNow;
    }
}
