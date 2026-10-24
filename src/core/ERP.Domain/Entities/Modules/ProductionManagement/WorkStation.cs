namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// A position inside a work centre where one operation is carried out.
/// </summary>
public sealed class WorkStation : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public string Location { get; private set; } = default!;
    public string? Description { get; private set; }

    // Required by Entity Framework to materialise the entity.
    private WorkStation() { }

    public WorkStation(string name, string location, string? description = null)
    {
        Name = name;
        Location = location;
        Description = description;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
}
