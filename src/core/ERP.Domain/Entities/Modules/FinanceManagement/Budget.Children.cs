namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class Budget
{
    public void SetBudgetLinePlannedAmount(Guid childId, Money plannedAmount)
    {
        EnsureDraft();
        var child = GetChild(childId, _lines);
        child.SetPlannedAmount(plannedAmount);
    }

    public void AssignBudgetLineCostCenter(Guid childId, Guid costCenterId)
    {
        EnsureDraft();
        var child = GetChild(childId, _lines);
        child.AssignCostCenter(costCenterId);
    }

    public void SetBudgetLineDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _lines);
        child.SetDescription(description);
    }

    public void RecordBudgetLineActual(Guid childId, Money amount)
    {
        var child = GetChild(childId, _lines);
        child.RecordActual(amount);
    }

    public void CommitBudgetLine(Guid childId, Money amount)
    {
        var child = GetChild(childId, _lines);
        child.Commit(amount);
    }

    public void ReleaseBudgetLineCommitment(Guid childId, Money amount)
    {
        var child = GetChild(childId, _lines);
        child.ReleaseCommitment(amount);
    }
}
