namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// One movement on a wallet, with the balance it left behind so the history can be audited.
/// </summary>
public sealed class WalletTransaction : BaseEntity
{
    public Wallet Wallet { get; private set; } = default!;
    public Guid WalletId { get; private set; }

    public WalletTransactionType Type { get; private set; }
    public Money Amount { get; private set; } = default!;
    public Money? BalanceAfter { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }

    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Whether the movement adds to the balance rather than taking from it.</summary>
    public bool IsCredit => Type is WalletTransactionType.TopUp
        or WalletTransactionType.RefundCredit
        or WalletTransactionType.PromotionalCredit
        or WalletTransactionType.Adjustment;

    private WalletTransaction() { }

    public WalletTransaction(Wallet wallet, WalletTransactionType type, Money amount)
    {
        ArgumentNullException.ThrowIfNull(wallet, nameof(wallet));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A wallet movement must carry a positive amount; the type decides its direction.");

        Wallet = wallet;
        WalletId = wallet.Id;
        Type = type;
        Amount = amount;
        OccurredOn = DateTimeOffset.UtcNow;
    }

    internal void SetBalanceAfter(Money balance)
    {
        ArgumentNullException.ThrowIfNull(balance, nameof(balance));
        BalanceAfter = balance;
    }

    internal void SetReference(string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
