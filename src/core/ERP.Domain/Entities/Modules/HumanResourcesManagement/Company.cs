namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A legal entity inside the group. Distinct from a branch: branches are places, companies
/// are the things that file their own accounts.
/// </summary>
public sealed class Company : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? Telephone { get; private set; }

    private Company() { }

    public Company(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        Name = name.Trim();
    }

    public void Update(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        Name = name.Trim();
    }
}
