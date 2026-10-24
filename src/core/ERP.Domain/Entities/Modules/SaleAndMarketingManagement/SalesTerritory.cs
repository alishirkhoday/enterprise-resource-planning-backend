namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// A geographic or segment area one team is answerable for, so customers and targets can be
/// divided up without argument.
/// </summary>
public sealed class SalesTerritory : AggregateRoot, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public SalesTerritory? Parent { get; private set; }
    public Guid? ParentId { get; private set; }

    private readonly List<SalesTerritory> _children = [];
    public IReadOnlyList<SalesTerritory> Children => _children.AsReadOnly();

    public Guid? ManagerHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>The cities this territory covers, when it is drawn geographically.</summary>
    private readonly List<Guid> _cityIds = [];
    public IReadOnlyList<Guid> CityIds => _cityIds.AsReadOnly();

    public bool IsActive { get; private set; }

    private SalesTerritory() { }

    public SalesTerritory(string code, string name, SalesTerritory? parent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        Parent = parent;
        ParentId = parent?.Id;
        IsActive = true;
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetParent(SalesTerritory? parent)
    {
        if (parent is not null && parent.Id == Id)
            throw new InvalidOperationException("A territory cannot be its own parent.");
        Parent = parent;
        ParentId = parent?.Id;
    }

    public void AssignManager(Guid humanId) => ManagerHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void CoverCity(Guid cityId)
    {
        if (cityId == Guid.Empty)
            throw new ArgumentException("CityId is empty.", nameof(cityId));
        if (_cityIds.Contains(cityId))
            return;
        _cityIds.Add(cityId);
    }

    public void RemoveCity(Guid cityId) => _cityIds.Remove(cityId);

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
