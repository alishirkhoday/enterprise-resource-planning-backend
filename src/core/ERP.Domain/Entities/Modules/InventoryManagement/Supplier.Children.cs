namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class Supplier
{
    public void SetSupplierContactContactInformation(Guid childId, string? mobilePhoneNumber, string? email, string? extension)
    {
        var child = GetChild(childId, _contacts);
        child.SetContactInformation(mobilePhoneNumber, email, extension);
    }

    public void SetSupplierContactJobTitle(Guid childId, string? jobTitle)
    {
        var child = GetChild(childId, _contacts);
        child.SetJobTitle(jobTitle);
    }

    public void SetSupplierContactDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _contacts);
        child.SetDescription(description);
    }

    public void MarkSupplierContactAsPrimary(Guid childId)
    {
        var child = GetChild(childId, _contacts);
        foreach (var sibling in _contacts.Where(sibling => sibling.Id != childId))
            sibling.UnmarkAsPrimary();
        child.MarkAsPrimary();
    }

    public void UnmarkSupplierContactAsPrimary(Guid childId)
    {
        var child = GetChild(childId, _contacts);
        child.UnmarkAsPrimary();
    }

    public void SetSupplierBankAccountInternationalBankAccountNumber(Guid childId, string? iban)
    {
        var child = GetChild(childId, _bankAccounts);
        child.SetInternationalBankAccountNumber(iban);
    }

    public void SetSupplierBankAccountAccountHolderName(Guid childId, string? holderName)
    {
        var child = GetChild(childId, _bankAccounts);
        child.SetAccountHolderName(holderName);
    }

    public void SetSupplierBankAccountDefault(Guid childId)
    {
        var child = GetChild(childId, _bankAccounts);
        foreach (var sibling in _bankAccounts.Where(sibling => sibling.Id != childId))
            sibling.UnsetDefault();
        child.SetDefault();
    }

    public void UnsetDefaultSupplierBankAccount(Guid childId)
    {
        var child = GetChild(childId, _bankAccounts);
        child.UnsetDefault();
    }

    public void SetSupplierEvaluationEvaluator(Guid childId, Guid humanId)
    {
        var child = GetChild(childId, _evaluations);
        child.SetEvaluator(humanId);
    }

    public void SetSupplierEvaluationComment(Guid childId, string? comment)
    {
        var child = GetChild(childId, _evaluations);
        child.SetComment(comment);
    }
}
