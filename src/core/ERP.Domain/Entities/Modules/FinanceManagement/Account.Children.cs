namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class Account
{
    public void MakeAccountDetailGroupRequired(Guid childId)
    {
        var child = GetChild(childId, _detailGroups);
        child.MakeRequired();
    }

    public void MakeAccountDetailGroupOptional(Guid childId)
    {
        var child = GetChild(childId, _detailGroups);
        child.MakeOptional();
    }
}
