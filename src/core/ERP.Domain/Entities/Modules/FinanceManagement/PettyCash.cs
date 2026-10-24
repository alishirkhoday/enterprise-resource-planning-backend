namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A float advanced to an employee for small expenses, topped back up to its ceiling once
/// the receipts are settled.
/// </summary>
public sealed class PettyCash : AggregateRoot, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string CurrencyCode { get; private set; } = default!;

    public Guid AccountId { get; private set; }
    public Guid? DetailAccountId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>The employee holding the float and answerable for it.</summary>
    public Guid HolderHumanId { get; private set; }

    /// <summary>The amount the float is replenished back to.</summary>
    public Money Ceiling { get; private set; } = default!;

    /// <summary>What is left of the float before the next replenishment.</summary>
    public Money CurrentBalance { get; private set; } = default!;

    public bool IsActive { get; private set; }

    private PettyCash() { }

    public PettyCash(string code, string name, Guid accountId, Guid holderHumanId, Money ceiling)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(ceiling, nameof(ceiling));
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is empty.", nameof(accountId));
        if (holderHumanId == Guid.Empty)
            throw new ArgumentException("HolderHumanId is empty.", nameof(holderHumanId));
        if (!ceiling.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(ceiling), "The ceiling must be positive.");

        Code = code.Trim();
        Name = name.Trim();
        AccountId = accountId;
        HolderHumanId = holderHumanId;
        Ceiling = ceiling;
        CurrencyCode = ceiling.CurrencyCode;
        CurrentBalance = Money.Zero(ceiling.CurrencyCode);
        IsActive = true;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void LinkToDetailAccount(Guid detailAccountId) => DetailAccountId = detailAccountId == Guid.Empty ? null : detailAccountId;

    public void ChangeHolder(Guid holderHumanId)
    {
        if (holderHumanId == Guid.Empty)
            throw new ArgumentException("HolderHumanId is empty.", nameof(holderHumanId));
        if (!CurrentBalance.IsZero)
            throw new InvalidOperationException("The float must be settled to zero before the holder changes.");
        HolderHumanId = holderHumanId;
    }

    public void RaiseCeiling(Money ceiling)
    {
        ArgumentNullException.ThrowIfNull(ceiling, nameof(ceiling));
        EnsureSameCurrency(ceiling);
        if (!ceiling.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(ceiling), "The ceiling must be positive.");
        Ceiling = ceiling;
    }

    /// <summary>Tops the float back up, never beyond its ceiling.</summary>
    public void Replenish(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        EnsureSameCurrency(amount);
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A replenishment must be positive.");

        var resulting = CurrentBalance.Add(amount);
        if (resulting.Amount > Ceiling.Amount)
            throw new InvalidOperationException($"A replenishment of {amount} would take the float above its ceiling of {Ceiling}.");
        CurrentBalance = resulting;
    }

    public void Spend(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        EnsureSameCurrency(amount);
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "An expense must be positive.");
        if (amount.Amount > CurrentBalance.Amount)
            throw new InvalidOperationException($"The float holds only {CurrentBalance}.");
        CurrentBalance = CurrentBalance.Subtract(amount);
    }

    public Money ReplenishmentNeeded() => Ceiling.Subtract(CurrentBalance);

    public void Close()
    {
        if (!CurrentBalance.IsZero)
            throw new InvalidOperationException("The float must be settled to zero before it is closed.");
        IsActive = false;
    }

    private void EnsureSameCurrency(Money amount)
    {
        if (amount.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"The amount must be expressed in {CurrencyCode}.", nameof(amount));
    }
}
