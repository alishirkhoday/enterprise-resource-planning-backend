namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class BankBranchAccount
{
    public void SetBankTransactionBankReference(Guid childId, string? bankReference, string? trackingCode)
    {
        var child = GetChild(childId, _transactions);
        child.SetBankReference(bankReference, trackingCode);
    }

    public void SetBankTransactionValueDate(Guid childId, DateOnly valueDate)
    {
        var child = GetChild(childId, _transactions);
        child.SetValueDate(valueDate);
    }

    public void SetBankTransactionBalanceAfter(Guid childId, Money balance)
    {
        var child = GetChild(childId, _transactions);
        child.SetBalanceAfter(balance);
    }

    public void SetBankTransactionDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _transactions);
        child.SetDescription(description);
    }

    public void LinkBankTransactionToDocument(Guid childId, Guid documentId)
    {
        var child = GetChild(childId, _transactions);
        child.LinkToDocument(documentId);
    }

    public void LinkBankTransactionToCheque(Guid childId, Guid chequeId)
    {
        var child = GetChild(childId, _transactions);
        child.LinkToCheque(chequeId);
    }

    public void MarkBankTransactionAsReconciled(Guid childId, Guid reconciliationId)
    {
        var child = GetChild(childId, _transactions);
        child.MarkAsReconciled(reconciliationId);
    }

    public void UnreconcileBankTransaction(Guid childId)
    {
        var child = GetChild(childId, _transactions);
        child.Unreconcile();
    }
}
