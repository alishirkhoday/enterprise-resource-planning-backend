namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Money given back against a payment, tracked from request through to the gateway confirming it.
/// </summary>
public sealed class Refund : BaseEntity
{
    public Payment Payment { get; private set; } = default!;
    public Guid PaymentId { get; private set; }

    public Money Amount { get; private set; } = default!;
    public RefundReason Reason { get; private set; }
    public RefundStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    public string? TransactionId { get; private set; }
    public Guid? ReturnRequestId { get; private set; }
    public Guid? SettlementId { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public string? Description { get; private set; }

    private Refund() { }

    public Refund(Payment payment, Money amount, RefundReason reason)
    {
        ArgumentNullException.ThrowIfNull(payment, nameof(payment));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A refund must be a positive amount.");
        if (amount.CurrencyCode != payment.Amount.CurrencyCode)
            throw new ArgumentException($"The refund must be expressed in {payment.Amount.CurrencyCode}.", nameof(amount));

        Payment = payment;
        PaymentId = payment.Id;
        Amount = amount;
        Reason = reason;
        RequestedAt = DateTimeOffset.UtcNow;
        Status = RefundStatus.Requested;
    }

    internal void LinkToReturnRequest(Guid returnRequestId) =>
        ReturnRequestId = returnRequestId == Guid.Empty ? null : returnRequestId;

    internal void SetRequester(Guid userId) => RequestedByUserId = userId == Guid.Empty ? null : userId;

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    internal void Approve()
    {
        if (Status != RefundStatus.Requested)
            throw new InvalidOperationException($"A refund in state {Status} cannot be approved.");
        Status = RefundStatus.Approved;
    }

    internal void MarkAsProcessed(string transactionId, Guid? settlementId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId, nameof(transactionId));
        if (Status != RefundStatus.Approved)
            throw new InvalidOperationException("Only an approved refund can be processed.");
        TransactionId = transactionId.Trim();
        SettlementId = settlementId == Guid.Empty ? null : settlementId;
        ProcessedAt = DateTimeOffset.UtcNow;
        Status = RefundStatus.Processed;
        Raise(new RefundProcessed(Id, PaymentId, Amount.Amount, Amount.CurrencyCode));
    }

    internal void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == RefundStatus.Processed)
            throw new InvalidOperationException("A processed refund cannot be rejected.");
        Status = RefundStatus.Rejected;
        Description = reason.Trim();
    }
}
