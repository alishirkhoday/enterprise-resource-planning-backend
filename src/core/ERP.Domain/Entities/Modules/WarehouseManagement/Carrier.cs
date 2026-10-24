namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// A company that moves goods on the organization behalf.
/// </summary>
public sealed class Carrier : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? ContactPerson { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public Address? Address { get; private set; }

    /// <summary>Template for building a tracking link, with the tracking number substituted in.</summary>
    public string? TrackingUrlTemplate { get; private set; }

    /// <summary>The subsidiary account freight charges are posted against.</summary>
    public Guid? DetailAccountId { get; private set; }

    public bool IsActive { get; private set; }
    public string? Description { get; private set; }

    private Carrier() { }

    public Carrier(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        IsActive = true;
    }

    public void Update(string name, string? contactPerson, string? phoneNumber, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        ContactPerson = string.IsNullOrWhiteSpace(contactPerson) ? null : contactPerson.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    public void SetAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        Address = address.Copy();
    }

    public void SetTrackingUrlTemplate(string? template) =>
        TrackingUrlTemplate = string.IsNullOrWhiteSpace(template) ? null : template.Trim();

    public void LinkToDetailAccount(Guid detailAccountId) =>
        DetailAccountId = detailAccountId == Guid.Empty ? null : detailAccountId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
