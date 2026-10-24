namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// What a qualification was in - رشته تحصیلی.
/// </summary>
public sealed class HumanEducationFieldOfStudy : AggregateRoot
{
    public string Name { get; private set; } = default!;

    private HumanEducationFieldOfStudy() { }

    public HumanEducationFieldOfStudy(string name)
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
