namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// A working shift - شیفت. What attendance, capacity and output are all measured against.
/// </summary>
public sealed class Shift : BaseEntity
{
    public string Name { get; private set; } = default!;
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public string? Description { get; private set; }

    // Required by Entity Framework to materialise the entity.
    private Shift() { }

    public Shift(string name, TimeSpan startTime, TimeSpan endTime, string? description = null)
    {
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        Description = description;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
}
