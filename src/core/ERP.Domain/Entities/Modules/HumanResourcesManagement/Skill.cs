namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A competency the organization recognises, used both to describe people and to state what a
/// position requires.
/// </summary>
public sealed class Skill : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Category { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private Skill() { }

    public Skill(string name, string? category = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        IsActive = true;
    }

    public void Update(string name, string? category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
