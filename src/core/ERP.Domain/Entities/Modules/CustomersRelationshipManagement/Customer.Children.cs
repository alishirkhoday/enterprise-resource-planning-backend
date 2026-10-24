namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

public sealed partial class Customer
{
    public void UpdateCustomerAddress(Guid childId, Address address, CustomerAddressType type)
    {
        var child = GetChild(childId, _addresses);
        child.Update(address, type);
    }

    public void SetCustomerAddressTitle(Guid childId, string? title)
    {
        var child = GetChild(childId, _addresses);
        child.SetTitle(title);
    }

    public void SetCustomerAddressDefault(Guid childId)
    {
        var child = GetChild(childId, _addresses);
        foreach (var sibling in _addresses.Where(sibling => sibling.Id != childId))
            sibling.UnsetDefault();
        child.SetDefault();
    }

    public void UnsetDefaultCustomerAddress(Guid childId)
    {
        var child = GetChild(childId, _addresses);
        child.UnsetDefault();
    }

    public void SetCustomerContactContactInformation(Guid childId, string? mobilePhoneNumber, string? email, string? extension)
    {
        var child = GetChild(childId, _contacts);
        child.SetContactInformation(mobilePhoneNumber, email, extension);
    }

    public void SetCustomerContactRole(Guid childId, string? jobTitle, string? department)
    {
        var child = GetChild(childId, _contacts);
        child.SetRole(jobTitle, department);
    }

    public void MarkCustomerContactAsDecisionMaker(Guid childId)
    {
        var child = GetChild(childId, _contacts);
        child.MarkAsDecisionMaker();
    }

    public void SetCustomerContactDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _contacts);
        child.SetDescription(description);
    }

    public void MarkCustomerContactAsPrimary(Guid childId)
    {
        var child = GetChild(childId, _contacts);
        foreach (var sibling in _contacts.Where(sibling => sibling.Id != childId))
            sibling.UnmarkAsPrimary();
        child.MarkAsPrimary();
    }

    public void UnmarkCustomerContactAsPrimary(Guid childId)
    {
        var child = GetChild(childId, _contacts);
        child.UnmarkAsPrimary();
    }
}
