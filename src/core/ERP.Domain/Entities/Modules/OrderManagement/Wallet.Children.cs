namespace ERP.Domain.Entities.Modules.OrderManagement;

public sealed partial class Wallet
{
    public void SetWalletTransactionReference(Guid childId, string referenceType, string referenceId)
    {
        var child = GetChild(childId, _transactions);
        child.SetReference(referenceType, referenceId);
    }

    public void SetWalletTransactionDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _transactions);
        child.SetDescription(description);
    }
}
