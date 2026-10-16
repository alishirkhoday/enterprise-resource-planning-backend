namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A line of the chart of accounts - حساب. The tree that says what a posting is, as against
/// the detail account which says who it is against.
/// 
/// Only a final account carries postings; the levels above it are groups that exist to be
/// summed. The code is not free text either: each type owns a block of the numbering, which is
/// what lets a trial balance be ordered and subtotalled by reading codes alone.
/// </summary>
public sealed class Account : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public AccountType Type { get; private set; }
    public AccountGroup Group { get; private set; }
    public AccountBalance Balance { get; private set; }
    public FinancialStatement FinancialStatement { get; private set; }

    /// <summary>A final account is one journal lines may be posted to; the rest only summarise.</summary>
    public bool IsFinal { get; private set; }

    public decimal Inventory { get; private set; }
    public bool IsSystematic { get; private set; }

    /// <summary>When set, the account only accepts amounts in this currency.</summary>
    public string? CurrencyCode { get; private set; }

    /// <summary>Blocks new postings without deleting the account or its history.</summary>
    public bool IsLocked { get; private set; }

    public string? Description { get; private set; }

    public Account? Parent { get; private set; }
    public Guid? ParentId { get; private set; }

    private readonly List<Account> _children = [];
    public IReadOnlyList<Account> Children => _children;

    private readonly List<AccountDetailGroup> _detailGroups = [];
    public IReadOnlyList<AccountDetailGroup> DetailGroups => _detailGroups;

    private Account() { }

    public Account(string code, string name, AccountType type, AccountGroup group, AccountBalance balance, FinancialStatement financialStatement, bool isFinal, Account? parent = null, bool isSystematic = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(code, nameof(code));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        Group = group;
        Balance = balance;
        FinancialStatement = financialStatement;
        IsFinal = isFinal;
        Parent = parent;
        ParentId = parent?.Id;
        IsSystematic = isSystematic;
    }

    /// <summary>
    /// Opens a heading in a type's own block: the top of a branch of the chart, always at level
    /// one, because nothing above it exists to hang it from.
    /// </summary>
    public static Account CreateTopLevel(
        string name,
        AccountType type,
        AccountBalance balance,
        FinancialStatement financialStatement,
        bool isFinal,
        int existingCountOfType)
    {
        var code = AccountCodeScheme.TopLevelCode(type, existingCountOfType);
        return new Account(code, name, type, AccountGroup.LevelOne, balance, financialStatement, isFinal);
    }

    /// <summary>
    /// Opens an account underneath this one, numbered from it.
    /// <para>
    /// The child's type is this account's type, not a choice. A chart where a liability sits under
    /// an asset still adds up line by line and is wrong in every report that groups by type, which
    /// is the kind of error nobody finds until a statement is already out.
    /// </para>
    /// </summary>
    public Account CreateChild(
        string name,
        AccountGroup group,
        AccountBalance balance,
        FinancialStatement financialStatement,
        bool isFinal,
        int existingChildCount)
    {
        if (IsFinal)
            throw new InvalidOperationException($"Account {Code} is final, so journal lines post to it and nothing may sit underneath it.");

        if (group <= Group)
            throw new InvalidOperationException($"An account under {Code} has to sit at a deeper level than {Group}.");

        var child = new Account(
            AccountCodeScheme.ChildCode(Code, existingChildCount),
            name,
            Type,
            group,
            balance,
            financialStatement,
            isFinal,
            this);

        _children.Add(child);
        return child;
    }

    public void Update(string code, string name, AccountType type, AccountGroup group, AccountBalance balance, FinancialStatement financialStatement, bool isFinal, Account? parent = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(code, nameof(code));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        if (IsSystematic)
            throw new InvalidOperationException("A systematic account cannot be modified.");
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        Group = group;
        Balance = balance;
        FinancialStatement = financialStatement;
        IsFinal = isFinal;
        Parent = parent;
        ParentId = parent?.Id;
    }

    public void SetInventory(decimal inventory)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inventory, nameof(inventory));
        Inventory = inventory;
    }

    public void RestrictToCurrency(string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
    }

    public void AllowAnyCurrency() => CurrencyCode = null;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Lock() => IsLocked = true;
    public void Unlock() => IsLocked = false;

    /// <summary>Declares a subsidiary group that journal lines on this account may carry.</summary>
    public void RequireDetailGroup(DetailAccountGroup group, short level, bool isRequired = true)
    {
        ArgumentNullException.ThrowIfNull(group, nameof(group));
        if (!IsFinal)
            throw new InvalidOperationException("Only a final account can carry subsidiary groups.");
        if (_detailGroups.Any(dg => dg.Level == level))
            throw new InvalidOperationException($"Subsidiary slot {level} is already assigned on this account.");
        _detailGroups.Add(new AccountDetailGroup(this, group, level, isRequired));
    }

    public void RemoveDetailGroup(short level)
    {
        var detailGroup = _detailGroups.FirstOrDefault(dg => dg.Level == level);
        if (detailGroup is not null)
            _detailGroups.Remove(detailGroup);
    }

    /// <summary>
    /// Checks the subsidiary accounts a journal line wants to use against what this account declares.
    /// </summary>
    public void EnsureDetailAccountsAreValid(DetailAccount? level1, DetailAccount? level2, DetailAccount? level3)
    {
        Validate(1, level1);
        Validate(2, level2);
        Validate(3, level3);

        void Validate(short level, DetailAccount? detailAccount)
        {
            var declared = _detailGroups.FirstOrDefault(dg => dg.Level == level);

            if (declared is null)
            {
                if (detailAccount is not null)
                    throw new InvalidOperationException($"Account {Code} does not accept a subsidiary account at level {level}.");
                return;
            }

            if (detailAccount is null)
            {
                if (declared.IsRequired)
                    throw new InvalidOperationException($"Account {Code} requires a subsidiary account at level {level}.");
                return;
            }

            if (detailAccount.GroupId != declared.DetailAccountGroupId)
                throw new InvalidOperationException($"The subsidiary account at level {level} does not belong to the group declared on account {Code}.");
        }
    }

    /// <summary>Whether a journal line may be posted to this account right now.</summary>
    public bool AcceptsPostings => IsFinal && !IsLocked && !IsDeleted;

    /// <summary>The signed effect of a debit on this account, given its normal balance.</summary>
    public decimal SignedAmount(decimal debtor, decimal creditor) =>
        Balance == AccountBalance.Debtor ? debtor - creditor : creditor - debtor;
}
