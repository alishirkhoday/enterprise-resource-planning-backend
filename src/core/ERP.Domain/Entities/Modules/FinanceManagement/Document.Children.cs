namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class Document
{
    public void SetDocumentItemDetailAccounts(Guid childId, DetailAccount? level1, DetailAccount? level2 = null, DetailAccount? level3 = null)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDetailAccounts(level1, level2, level3);
    }

    public void SetDocumentItemForeignAmount(Guid childId, Money foreignAmount, decimal exchangeRate)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetForeignAmount(foreignAmount, exchangeRate);
    }

    public void AssignDocumentItemCostCenter(Guid childId, Guid costCenterId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.AssignCostCenter(costCenterId);
    }

    public void AssignDocumentItemProject(Guid childId, Guid projectId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.AssignProject(projectId);
    }

    public void SetDocumentItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }
}
