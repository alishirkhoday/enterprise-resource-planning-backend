namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class FixedAsset
{
    public void SetDepreciationEntryUnitsProduced(Guid childId, decimal units)
    {
        var child = GetChild(childId, _depreciationEntries);
        child.SetUnitsProduced(units);
    }

    public void LinkDepreciationEntryToDocument(Guid childId, Guid documentId)
    {
        var child = GetChild(childId, _depreciationEntries);
        child.LinkToDocument(documentId);
    }

    public void LinkDepreciationEntryToAccountingPeriod(Guid childId, Guid accountingPeriodId)
    {
        var child = GetChild(childId, _depreciationEntries);
        child.LinkToAccountingPeriod(accountingPeriodId);
    }
}
