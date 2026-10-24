namespace ERP.Domain.Entities.Notifications;

/// <summary>
/// A side effect that must happen exactly once after the transaction that produced it commits.
/// Writing it in the same transaction as the business change is what makes delivery reliable.
/// </summary>
public sealed class OutboxMessage : AggregateRoot
{
    /// <summary>Fully qualified name of the payload type.</summary>
    public string MessageType { get; private set; } = default!;

    /// <summary>Serialized payload, normally JSON.</summary>
    public string Payload { get; private set; } = default!;

    public OutboxMessageStatus Status { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public DateTimeOffset? NextAttemptOn { get; private set; }

    public int AttemptCount { get; private set; }
    public string? Error { get; private set; }
    public string? CorrelationId { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string messageType, string payload, string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType, nameof(messageType));
        ArgumentException.ThrowIfNullOrWhiteSpace(payload, nameof(payload));
        MessageType = messageType.Trim();
        Payload = payload;
        CorrelationId = correlationId;
        Status = OutboxMessageStatus.Pending;
        OccurredOn = DateTimeOffset.UtcNow;
    }

    public void MarkAsProcessing() => Status = OutboxMessageStatus.Processing;

    public void MarkAsProcessed()
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedOn = DateTimeOffset.UtcNow;
        Error = null;
        NextAttemptOn = null;
    }

    /// <summary>
    /// Records a failed attempt and backs off exponentially, giving up after <paramref name="maxAttempts"/>.
    /// </summary>
    public void MarkAsFailed(string error, int maxAttempts = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error, nameof(error));
        AttemptCount++;
        Error = error;

        if (AttemptCount >= maxAttempts)
        {
            Status = OutboxMessageStatus.Abandoned;
            NextAttemptOn = null;
            return;
        }

        Status = OutboxMessageStatus.Pending;
        NextAttemptOn = DateTimeOffset.UtcNow.AddSeconds(Math.Pow(2, AttemptCount) * 10);
    }
}
