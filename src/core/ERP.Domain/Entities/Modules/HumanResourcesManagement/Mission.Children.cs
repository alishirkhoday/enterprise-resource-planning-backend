namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

public sealed partial class Mission
{
    public void SetMissionExpenseDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _expenses);
        child.SetDescription(description);
    }

    public void AttachReceiptMissionExpense(Guid childId, Guid attachmentId)
    {
        var child = GetChild(childId, _expenses);
        child.AttachReceipt(attachmentId);
    }

    public void MarkMissionExpenseAsReimbursed(Guid childId, DateOnly reimbursedOn)
    {
        var child = GetChild(childId, _expenses);
        child.MarkAsReimbursed(reimbursedOn);
    }
}
