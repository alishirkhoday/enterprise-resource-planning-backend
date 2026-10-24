namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class BankReconciliation
{
    public void LinkBankReconciliationItemToBankTransaction(Guid childId, Guid bankTransactionId)
    {
        var child = GetChild(childId, _items);
        child.LinkToBankTransaction(bankTransactionId);
    }

    public void LinkBankReconciliationItemToDocument(Guid childId, Guid documentId)
    {
        var child = GetChild(childId, _items);
        child.LinkToDocument(documentId);
    }

    public void LinkBankReconciliationItemToCheque(Guid childId, Guid chequeId)
    {
        var child = GetChild(childId, _items);
        child.LinkToCheque(chequeId);
    }
}
