namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Declares that a ledger account may, or must, be posted against a member of one subsidiary group,
/// and in which of the three subsidiary slots a journal line should carry it.
/// </summary>
public sealed class AccountDetailGroup : BaseEntity
{
    public Account Account { get; private set; } = default!;
    public Guid AccountId { get; private set; }

    public DetailAccountGroup DetailAccountGroup { get; private set; } = default!;
    public Guid DetailAccountGroupId { get; private set; }

    /// <summary>Which subsidiary slot of a journal line this group fills. One, two or three.</summary>
    public short Level { get; private set; }

    /// <summary>When true a journal line on this account is rejected unless the slot is filled.</summary>
    public bool IsRequired { get; private set; }

    private AccountDetailGroup() { }

    public AccountDetailGroup(Account account, DetailAccountGroup detailAccountGroup, short level, bool isRequired = true)
    {
        ArgumentNullException.ThrowIfNull(account, nameof(account));
        ArgumentNullException.ThrowIfNull(detailAccountGroup, nameof(detailAccountGroup));
        if (level is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(level), "Level must be 1, 2 or 3.");

        Account = account;
        AccountId = account.Id;
        DetailAccountGroup = detailAccountGroup;
        DetailAccountGroupId = detailAccountGroup.Id;
        Level = level;
        IsRequired = isRequired;
    }

    internal void MakeRequired() => IsRequired = true;
    internal void MakeOptional() => IsRequired = false;
}
