namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Where a qualification was earned.
/// </summary>
public sealed class HumanEducationUniversity : BaseEntity
{
    public string Name { get; private set; } = default!;

    private HumanEducationUniversity() { }

    public HumanEducationUniversity(string name)
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
