namespace ERP.Domain.Entities.Modules.OrderManagement;

public sealed partial class InstallmentPlan
{
    public void ApplyInstallmentLateFee(Guid childId, Money lateFee)
    {
        var child = GetChild(childId, _installments);
        child.ApplyLateFee(lateFee);
    }
}
