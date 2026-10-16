namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A seat in the organization chart. Positions report to positions, so the chart survives
/// the people who occupy them coming and going.
/// </summary>
public sealed class Position : BaseEntity
{
    public Department Department { get; private set; } = default!;
    public Guid DepartmentId { get; private set; }

    public string Title { get; private set; } = default!;
    public string? Code { get; private set; }
    public bool IsSystematic { get; private set; }
    public string? Description { get; private set; }

    public Position? ReportsTo { get; private set; }
    public Guid? ReportsToPositionId { get; private set; }

    /// <summary>How many people this position may hold at once.</summary>
    public int Headcount { get; private set; }

    /// <summary>Marks the position as managing its department, used by approval workflows.</summary>
    public bool IsDepartmentHead { get; private set; }

    public bool IsActive { get; private set; }

    private readonly List<Position> _subordinates = [];
    public IReadOnlyList<Position> Subordinates => _subordinates;

    private Position() { }

    public Position(Department department, string title, int headcount = 1, bool isSystematic = false)
    {
        ArgumentNullException.ThrowIfNull(department, nameof(department));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(headcount, nameof(headcount));
        Department = department;
        DepartmentId = department.Id;
        Title = title.Trim();
        Headcount = headcount;
        IsSystematic = isSystematic;
        IsActive = true;
    }

    public void Update(string title, int headcount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(headcount, nameof(headcount));
        Title = title.Trim();
        Headcount = headcount;
    }

    public void SetCode(string? code) => Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void SetReportsTo(Position? position)
    {
        if (position is not null && position.Id == Id)
            throw new InvalidOperationException("A position cannot report to itself.");
        ReportsTo = position;
        ReportsToPositionId = position?.Id;
    }

    public void MarkAsDepartmentHead() => IsDepartmentHead = true;
    public void UnmarkAsDepartmentHead() => IsDepartmentHead = false;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
