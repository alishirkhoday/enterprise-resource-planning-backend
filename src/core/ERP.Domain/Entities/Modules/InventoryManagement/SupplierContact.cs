namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A person to deal with at a supplier. An organization supplier normally has several.
/// </summary>
public sealed class SupplierContact : BaseEntity
{
    public Supplier Supplier { get; private set; } = default!;
    public Guid SupplierId { get; private set; }

    public string FullName { get; private set; } = default!;
    public string? JobTitle { get; private set; }
    public string? MobilePhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Extension { get; private set; }
    public bool IsPrimary { get; private set; }
    public string? Description { get; private set; }

    private SupplierContact() { }

    public SupplierContact(Supplier supplier, string fullName)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        Supplier = supplier;
        SupplierId = supplier.Id;
        FullName = fullName.Trim();
    }

    internal void SetContactInformation(string? mobilePhoneNumber, string? email, string? extension)
    {
        MobilePhoneNumber = string.IsNullOrWhiteSpace(mobilePhoneNumber) ? null : mobilePhoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Extension = string.IsNullOrWhiteSpace(extension) ? null : extension.Trim();
    }

    internal void SetJobTitle(string? jobTitle) => JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle.Trim();

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    internal void MarkAsPrimary() => IsPrimary = true;
    internal void UnmarkAsPrimary() => IsPrimary = false;
}
