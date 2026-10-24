namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A branch of a bank. Accounts are held at branches rather than at banks, and a cheque names
/// the branch it is drawn on.
/// </summary>
public sealed class BankBranch : AggregateRoot
{
    public Bank Bank { get; private set; } = default!;
    public Guid BankId { get; private set; }
    public string Name { get; private set; } = default!;

    private readonly List<BankBranchAccount> _accounts = [];
    public IReadOnlyList<BankBranchAccount> Accounts => _accounts.AsReadOnly();

    private BankBranch() { }

    public BankBranch(Bank bank, string name)
    {
        ArgumentNullException.ThrowIfNull(bank, nameof(bank));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Bank = bank;
        BankId = bank.Id;
        Name = name.Trim();
    }

    public void Update(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
    }

    public void AddAccount(BankBranchAccount account) => _accounts.Add(account);
}
