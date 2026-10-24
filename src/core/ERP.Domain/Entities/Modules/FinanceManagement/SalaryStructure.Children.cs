namespace ERP.Domain.Entities.Modules.FinanceManagement;

public sealed partial class SalaryStructure
{
    public void SetSalaryStructureItemFixedAmount(Guid childId, Money amount)
    {
        var child = GetChild(childId, _items);
        child.SetFixedAmount(amount);
    }

    public void SetSalaryStructureItemRate(Guid childId, Percentage rate, string? baseItemCode = null)
    {
        var child = GetChild(childId, _items);
        child.SetRate(rate, baseItemCode);
    }

    public void SetSalaryStructureItemTaxTreatment(Guid childId, bool isTaxable, bool isInsurable)
    {
        var child = GetChild(childId, _items);
        child.SetTaxTreatment(isTaxable, isInsurable);
    }

    public void LinkSalaryStructureItemAccount(Guid childId, Guid accountId)
    {
        var child = GetChild(childId, _items);
        child.LinkAccount(accountId);
    }

    public void SetSalaryStructureItemDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }
}
