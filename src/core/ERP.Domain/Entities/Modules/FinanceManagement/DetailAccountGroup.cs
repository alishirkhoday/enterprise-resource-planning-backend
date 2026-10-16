namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A family of subsidiary accounts, such as Customers or Employees. Ledger accounts declare
/// which groups may be used against them, which is what keeps a receivable posted against a
/// customer rather than against a machine.
/// </summary>
public sealed class DetailAccountGroup : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    /// <summary>
    /// The entity type members of this group stand for, for example Customer. When set, a member
    /// is expected to carry the identifier of a real record.
    /// </summary>
    public string? ReferenceType { get; private set; }

    public string? Description { get; private set; }
    public bool IsSystematic { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<DetailAccount> _accounts = [];
    public IReadOnlyList<DetailAccount> Accounts => _accounts;

    private DetailAccountGroup() { }

    public DetailAccountGroup(string code, string name, string? referenceType = null, bool isSystematic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        ReferenceType = string.IsNullOrWhiteSpace(referenceType) ? null : referenceType.Trim();
        IsSystematic = isSystematic;
        IsActive = true;
    }

    public void Update(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Activate() => IsActive = true;

    public void Deactivate()
    {
        if (IsSystematic)
            throw new InvalidOperationException("A systematic detail account group cannot be deactivated.");
        IsActive = false;
    }
}
