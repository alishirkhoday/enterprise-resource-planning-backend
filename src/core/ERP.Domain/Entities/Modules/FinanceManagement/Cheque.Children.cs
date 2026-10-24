namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class Cheque
{
    public void LinkChequeOperationToDocument(Guid childId, Guid documentId)
    {
        var child = GetChild(childId, _operations);
        child.LinkToDocument(documentId);
    }
}
