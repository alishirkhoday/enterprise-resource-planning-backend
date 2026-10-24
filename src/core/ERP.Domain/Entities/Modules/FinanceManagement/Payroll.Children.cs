namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class Payroll
{
    public void SetPayrollItemTaxTreatment(Guid childId, bool isTaxable, bool isInsurable)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetTaxTreatment(isTaxable, isInsurable);
    }

    public void SetPayrollItemQuantity(Guid childId, decimal quantity, Money unitRate)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetQuantity(quantity, unitRate);
    }

    public void LinkPayrollItemToStructureItem(Guid childId, Guid salaryStructureItemId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.LinkToStructureItem(salaryStructureItemId);
    }

    public void LinkPayrollItemAccount(Guid childId, Guid accountId)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.LinkAccount(accountId);
    }

    public void SetPayrollItemDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }

    public void SetPayrollRewardDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _rewards);
        child.SetDescription(description);
    }

    public void SetPayrollDeductionDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _deductions);
        child.SetDescription(description);
    }
}
