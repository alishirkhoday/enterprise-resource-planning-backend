namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class FinancialYear
{
    public void CloseAccountingPeriod(Guid childId, string? closedBy = null)
    {
        var child = GetChild(childId, _periods);
        child.Close(closedBy);
    }

    public void ReopenAccountingPeriod(Guid childId, string? reopenedBy = null)
    {
        var child = GetChild(childId, _periods);
        child.Reopen(reopenedBy);
    }

    public void LockAccountingPeriod(Guid childId)
    {
        var child = GetChild(childId, _periods);
        child.Lock();
    }

    public void UnlockAccountingPeriod(Guid childId)
    {
        var child = GetChild(childId, _periods);
        child.Unlock();
    }
}
