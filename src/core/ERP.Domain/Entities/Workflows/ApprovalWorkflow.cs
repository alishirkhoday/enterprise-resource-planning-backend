namespace ERP.Domain.Entities.Workflows;

/// <summary>
/// The ordered chain of approvals a document type has to pass. Defining it as data rather than
/// code lets an organization change who signs off without a release.
/// </summary>
public sealed class ApprovalWorkflow : BaseEntity, IBranchScoped
{
    public string Key { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    /// <summary>The entity this workflow governs, for example PurchaseOrder.</summary>
    public string EntityType { get; private set; } = default!;

    public string? Description { get; private set; }
    public Guid? BranchId { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<ApprovalWorkflowStep> _steps = [];
    public IReadOnlyList<ApprovalWorkflowStep> Steps => _steps;

    private ApprovalWorkflow() { }

    public ApprovalWorkflow(string key, string name, string entityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType, nameof(entityType));
        Key = key.Trim();
        Name = name.Trim();
        EntityType = entityType.Trim();
        IsActive = true;
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddStep(ApprovalWorkflowStep step)
    {
        ArgumentNullException.ThrowIfNull(step, nameof(step));
        if (_steps.Any(s => s.Order == step.Order))
            throw new InvalidOperationException($"A step with order {step.Order} already exists in this workflow.");
        _steps.Add(step);
    }

    public void RemoveStep(int order)
    {
        var step = _steps.FirstOrDefault(s => s.Order == order);
        if (step is not null)
            _steps.Remove(step);
    }

    public ApprovalWorkflowStep? FirstStep() => _steps.OrderBy(s => s.Order).FirstOrDefault();

    public ApprovalWorkflowStep? StepAfter(int order) => _steps.Where(s => s.Order > order).OrderBy(s => s.Order).FirstOrDefault();

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
