using ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;

namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// One attempt to pay for an order, with what the gateway said about it and what has since been
/// refunded.
/// </summary>
public sealed class Payment : BaseEntity
{
    public Order Order { get; private set; } = default!;
    public Guid OrderId { get; private set; }

    public PaymentStatus Status { get; private set; }
    public PaymentMethod Method { get; private set; }

    public Money Amount { get; private set; } = default!;
    public Money? AmountPaid { get; private set; }
    public Money RefundedAmount { get; private set; } = default!;

    public DateTimeOffset? PaymentDateTime { get; private set; }
    public PaymentGatewayInformation PaymentGatewayInformation { get; private set; } = default!;

    /// <summary>The identifier the gateway assigns the attempt.</summary>
    public string? TransactionId { get; private set; }
    public string? AuthorityCode { get; private set; }
    public string? FailureReason { get; private set; }

    public Guid? InstallmentPlanId { get; private set; }
    public Guid? SettlementId { get; private set; }
    public Guid? WalletId { get; private set; }

    private readonly List<Refund> _refunds = [];
    public IReadOnlyList<Refund> Refunds => _refunds.AsReadOnly();

    private Payment() { }

    public Payment(Order order, PaymentMethod method, Money amount)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A payment must be a positive amount.");
        if (amount.CurrencyCode != order.CurrencyCode)
            throw new ArgumentException($"The payment must be expressed in {order.CurrencyCode}.", nameof(amount));

        Order = order;
        OrderId = order.Id;
        Method = method;
        Amount = amount;
        RefundedAmount = Money.Zero(amount.CurrencyCode);
        Status = PaymentStatus.Pending;
    }

    internal void SetGatewayInformation(PaymentGatewayInformation information)
    {
        ArgumentNullException.ThrowIfNull(information, nameof(information));
        PaymentGatewayInformation = information;
    }

    internal void SetAuthority(string authorityCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorityCode, nameof(authorityCode));
        AuthorityCode = authorityCode.Trim();
    }

    internal void LinkToInstallmentPlan(Guid installmentPlanId) =>
        InstallmentPlanId = installmentPlanId == Guid.Empty ? null : installmentPlanId;

    internal void LinkToWallet(Guid walletId) => WalletId = walletId == Guid.Empty ? null : walletId;

    internal void MarkAsSucceeded(Money amountPaid, string transactionId)
    {
        ArgumentNullException.ThrowIfNull(amountPaid, nameof(amountPaid));
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId, nameof(transactionId));
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"A payment in state {Status} cannot succeed.");
        if (amountPaid.CurrencyCode != Amount.CurrencyCode)
            throw new ArgumentException($"The amount must be expressed in {Amount.CurrencyCode}.", nameof(amountPaid));

        if (!amountPaid.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amountPaid), "The amount paid must be positive.");
        AmountPaid = amountPaid;
        TransactionId = transactionId.Trim();
        PaymentDateTime = DateTimeOffset.UtcNow;
        Status = PaymentStatus.Succeeded;
        FailureReason = null;
        Raise(new PaymentSucceeded(Id, OrderId, amountPaid.Amount, amountPaid.CurrencyCode));
    }

    internal void MarkAsFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"A payment in state {Status} cannot fail.");
        Status = PaymentStatus.Failed;
        FailureReason = reason.Trim();
        Raise(new PaymentFailed(Id, OrderId, FailureReason));
    }

    internal void LinkToSettlement(Guid settlementId) => SettlementId = settlementId == Guid.Empty ? null : settlementId;

    internal void AddRefund(Refund refund)
    {
        ArgumentNullException.ThrowIfNull(refund);
        EnsureCanAddChild(refund, refund.PaymentId, _refunds);

        if (!IsRefundable)
            throw new InvalidOperationException("Only a successful payment with a remaining balance can be refunded.");
        if (refund.Amount.Amount > RefundableAmount.Amount)
            throw new InvalidOperationException($"Only {RefundableAmount} is still refundable.");

        var refundedAmount = RefundedAmount.Add(refund.Amount);
        _refunds.Add(refund);
        RefundedAmount = refundedAmount;
        Status = RefundedAmount.Amount >= (AmountPaid?.Amount ?? 0m)
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
    }

    public Money RefundableAmount =>
        AmountPaid is null ? Money.Zero(Amount.CurrencyCode) : AmountPaid.Subtract(RefundedAmount);

    public bool IsRefundable => Status is PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded
                                && RefundableAmount.IsPositive;
}
