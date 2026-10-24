namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// A person to deal with at an organization customer.
/// </summary>
public sealed class CustomerContact : BaseEntity
{
    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public string FullName { get; private set; } = default!;
    public string? JobTitle { get; private set; }
    public string? Department { get; private set; }
    public string? MobilePhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Extension { get; private set; }

    /// <summary>Whether this contact is the one who signs off on purchases.</summary>
    public bool IsDecisionMaker { get; private set; }

    public bool IsPrimary { get; private set; }
    public string? Description { get; private set; }

    private CustomerContact() { }

    public CustomerContact(Customer customer, string fullName)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        Customer = customer;
        CustomerId = customer.Id;
        FullName = fullName.Trim();
    }

    internal void SetContactInformation(string? mobilePhoneNumber, string? email, string? extension)
    {
        MobilePhoneNumber = string.IsNullOrWhiteSpace(mobilePhoneNumber) ? null : mobilePhoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Extension = string.IsNullOrWhiteSpace(extension) ? null : extension.Trim();
    }

    internal void SetRole(string? jobTitle, string? department)
    {
        JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle.Trim();
        Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
    }

    internal void MarkAsDecisionMaker() => IsDecisionMaker = true;

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    internal void MarkAsPrimary() => IsPrimary = true;
    internal void UnmarkAsPrimary() => IsPrimary = false;
}
