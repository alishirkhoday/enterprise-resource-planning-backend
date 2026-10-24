namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A till holding physical cash, tied to the ledger account its balance is carried in.
/// </summary>
public sealed class CashBox : AggregateRoot, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string CurrencyCode { get; private set; } = default!;

    /// <summary>The ledger account this till reports into.</summary>
    public Guid AccountId { get; private set; }

    public Guid? DetailAccountId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>The employee answerable for the cash in it.</summary>
    public Guid? ResponsibleHumanId { get; private set; }

    /// <summary>The most cash the till is allowed to hold, when a limit is imposed.</summary>
    public Money? Ceiling { get; private set; }

    public string? Location { get; private set; }
    public bool IsActive { get; private set; }

    private CashBox() { }

    public CashBox(string code, string name, string currencyCode, Guid accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is empty.", nameof(accountId));

        Code = code.Trim();
        Name = name.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        AccountId = accountId;
        IsActive = true;
    }

    public void Update(string name, string? location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
    }

    public void AssignResponsible(Guid humanId) => ResponsibleHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void LinkToDetailAccount(Guid detailAccountId) => DetailAccountId = detailAccountId == Guid.Empty ? null : detailAccountId;

    public void SetCeiling(Money ceiling)
    {
        ArgumentNullException.ThrowIfNull(ceiling, nameof(ceiling));
        if (ceiling.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"The ceiling must be expressed in {CurrencyCode}.", nameof(ceiling));
        if (!ceiling.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(ceiling), "The ceiling must be positive.");
        Ceiling = ceiling;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
