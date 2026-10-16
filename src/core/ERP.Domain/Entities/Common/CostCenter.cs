namespace ERP.Domain.Entities.Common;

/// <summary>
/// A unit that costs are collected against. Cost centres are cross cutting: production, payroll,
/// purchasing and general ledger entries all point at one.
/// </summary>
public sealed class CostCenter : BaseEntity, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public CostCenterType Type { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public Guid? BranchId { get; private set; }

    public CostCenter? Parent { get; private set; }
    public Guid? ParentId { get; private set; }

    private readonly List<CostCenter> _children = [];
    public IReadOnlyList<CostCenter> Children => _children;

    private CostCenter() { }

    public CostCenter(string code, string name, CostCenterType type, CostCenter? parent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        Parent = parent;
        ParentId = parent?.Id;
        IsActive = true;
    }

    public void Update(string code, string name, CostCenterType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
    }

    public void SetParent(CostCenter? parent)
    {
        if (parent is not null && parent.Id == Id)
            throw new InvalidOperationException("A cost center cannot be its own parent.");
        Parent = parent;
        ParentId = parent?.Id;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
