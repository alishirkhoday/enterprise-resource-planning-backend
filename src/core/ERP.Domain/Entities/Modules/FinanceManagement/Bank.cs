namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A bank the business deals with.
/// </summary>
public sealed class Bank : AggregateRoot
{
    public string Name { get; private set; } = default!;

    private readonly List<BankBranch> _branches = [];
    public IReadOnlyList<BankBranch> Branches => _branches.AsReadOnly();

    private Bank() { }

    public Bank(string name)
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
