namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// What a qualification was in - رشته تحصیلی.
/// </summary>
public sealed class HumanEducationFieldOfStudy : BaseEntity
{
    public string Name { get; private set; } = default!;

    private HumanEducationFieldOfStudy() { }

    public HumanEducationFieldOfStudy(string name)
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
