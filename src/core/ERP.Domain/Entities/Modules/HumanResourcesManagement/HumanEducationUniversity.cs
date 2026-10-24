namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Where a qualification was earned.
/// </summary>
public sealed class HumanEducationUniversity : AggregateRoot
{
    public string Name { get; private set; } = default!;

    private HumanEducationUniversity() { }

    public HumanEducationUniversity(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
    }

    public void Update(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
    }
}
