using ERP.Domain.Entities.Organizations;

namespace ERP.Domain.Entities.Branches;

/// <summary>
/// A place the organization operates from. Documents, stock and people are all scoped to one,
/// which is what lets a group run several sites out of one database.
/// </summary>
public sealed class Branch : AggregateRoot
{
    public Organization Organization { get; private set; } = default!;
    public Guid OrganizationId { get; private set; }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public BranchType Type { get; private set; }

    public Address Address { get; private set; } = default!;
    public string? Telephone { get; private set; }
    public string? Email { get; private set; }

    public Guid? ManagerHumanId { get; private set; }
    public Guid? DefaultWarehouseId { get; private set; }
    public Guid? DefaultCostCenterId { get; private set; }
    public Guid? WorkCalendarId { get; private set; }

    /// <summary>Whether this branch keeps its own set of books.</summary>
    public bool HasOwnBooks { get; private set; }

    public bool IsHeadOffice { get; private set; }
    public bool IsActive { get; private set; }
    public string? Description { get; private set; }

    private Branch() { }

    public Branch(Organization organization, string code, string name, BranchType type, Address address)
    {
        ArgumentNullException.ThrowIfNull(organization, nameof(organization));
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Organization = organization;
        OrganizationId = organization.Id;
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        Address = address.Copy();
        IsActive = true;
    }

    public void Update(string name, BranchType type, Address address)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Type = type;
        Address = address.Copy();
    }

    public void SetContactInformation(string? telephone, string? email)
    {
        Telephone = string.IsNullOrWhiteSpace(telephone) ? null : telephone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    public void AssignManager(Guid humanId) => ManagerHumanId = humanId == Guid.Empty ? null : humanId;

    public void SetDefaults(Guid? warehouseId, Guid? costCenterId, Guid? workCalendarId)
    {
        DefaultWarehouseId = warehouseId == Guid.Empty ? null : warehouseId;
        DefaultCostCenterId = costCenterId == Guid.Empty ? null : costCenterId;
        WorkCalendarId = workCalendarId == Guid.Empty ? null : workCalendarId;
    }

    public void MarkAsHeadOffice() => IsHeadOffice = true;

    public void EnableOwnBooks() => HasOwnBooks = true;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Activate() => IsActive = true;

    public void Deactivate()
    {
        if (IsHeadOffice)
            throw new InvalidOperationException("The head office cannot be deactivated.");
        IsActive = false;
    }
}
