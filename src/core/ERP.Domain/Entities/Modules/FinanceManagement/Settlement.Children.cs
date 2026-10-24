namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class Settlement
{
    public void SetSettlementAllocationDiscountTaken(Guid childId, Money discount)
    {
        EnsureDraft();
        var child = GetChild(childId, _allocations);
        child.SetDiscountTaken(discount);
    }

    public void SetSettlementAllocationDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _allocations);
        child.SetDescription(description);
    }
}
