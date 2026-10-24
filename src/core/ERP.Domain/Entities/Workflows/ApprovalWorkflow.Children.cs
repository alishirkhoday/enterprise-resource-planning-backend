namespace ERP.Domain.Entities.Workflows;

public sealed partial class ApprovalWorkflow
{
    public void SetApprovalWorkflowStepMinimumAmount(Guid childId, decimal? minimumAmount)
    {
        var child = GetChild(childId, _steps);
        child.SetMinimumAmount(minimumAmount);
    }

    public void AllowApprovalWorkflowStepRejection(Guid childId)
    {
        var child = GetChild(childId, _steps);
        child.AllowRejection();
    }

    public void ForbidApprovalWorkflowStepRejection(Guid childId)
    {
        var child = GetChild(childId, _steps);
        child.ForbidRejection();
    }
}
