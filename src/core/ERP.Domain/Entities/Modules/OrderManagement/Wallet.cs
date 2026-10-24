using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// A customer stored balance. The balance is kept alongside the transactions that produced it,
/// so it can always be proved.
/// </summary>
public sealed partial class Wallet : AggregateRoot
{
    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public Money Balance { get; private set; } = default!;

    /// <summary>Held against pending orders, so the same money cannot be spent twice.</summary>
    public Money BlockedAmount { get; private set; } = default!;

    public Money AvailableBalance => Balance.Subtract(BlockedAmount);

    public bool IsActive { get; private set; }
    public Guid? DetailAccountId { get; private set; }

    private readonly List<WalletTransaction> _transactions = [];
    public IReadOnlyList<WalletTransaction> Transactions => _transactions.AsReadOnly();

    private Wallet() { }

    public Wallet(Customer customer, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        Customer = customer;
        CustomerId = customer.Id;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Balance = Money.Zero(CurrencyCode);
        BlockedAmount = Money.Zero(CurrencyCode);
        IsActive = true;
    }

    public void LinkToDetailAccount(Guid detailAccountId) =>
        DetailAccountId = detailAccountId == Guid.Empty ? null : detailAccountId;

    public void Credit(WalletTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureCanAddChild(transaction, transaction.WalletId, _transactions);

        EnsureActive();
        EnsureSameCurrency(transaction.Amount);
        if (!transaction.IsCredit)
            throw new ArgumentException("A credit transaction was expected.", nameof(transaction));

        Balance = Balance.Add(transaction.Amount);
        transaction.SetBalanceAfter(Balance);
        _transactions.Add(transaction);
    }

    public void Debit(WalletTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureCanAddChild(transaction, transaction.WalletId, _transactions);

        EnsureActive();
        EnsureSameCurrency(transaction.Amount);
        if (transaction.IsCredit)
            throw new ArgumentException("A debit transaction was expected.", nameof(transaction));
        if (transaction.Amount.Amount > AvailableBalance.Amount)
            throw new InvalidOperationException($"The wallet has only {AvailableBalance} available.");

        Balance = Balance.Subtract(transaction.Amount);
        transaction.SetBalanceAfter(Balance);
        _transactions.Add(transaction);
    }

    /// <summary>Holds funds against a pending order.</summary>
    public void Block(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        EnsureActive();
        EnsureSameCurrency(amount);
        if (amount.Amount > AvailableBalance.Amount)
            throw new InvalidOperationException($"The wallet has only {AvailableBalance} available.");
        BlockedAmount = BlockedAmount.Add(amount);
    }

    public void Release(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        EnsureSameCurrency(amount);
        if (amount.Amount > BlockedAmount.Amount)
            throw new InvalidOperationException("Cannot release more than is blocked.");
        BlockedAmount = BlockedAmount.Subtract(amount);
    }

    public void Freeze() => IsActive = false;
    public void Unfreeze() => IsActive = true;

    private void EnsureActive()
    {
        if (!IsActive)
            throw new InvalidOperationException("The wallet is frozen.");
    }

    private void EnsureSameCurrency(Money amount)
    {
        if (amount.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"The amount must be expressed in {CurrencyCode}.", nameof(amount));
    }
}
