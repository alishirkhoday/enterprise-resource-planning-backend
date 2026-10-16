namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A bank the business deals with.
/// </summary>
public sealed class Bank : BaseEntity
{
    public string Name { get; private set; } = default!;

    private readonly List<BankBranch> _branches = [];
    public IReadOnlyList<BankBranch> Branches => _branches;

    private Bank() { }

    public Bank(string name)
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
