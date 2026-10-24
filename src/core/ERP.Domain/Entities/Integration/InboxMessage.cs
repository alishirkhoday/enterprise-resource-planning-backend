namespace ERP.Domain.Entities.Integration;

/// <summary>
/// One message that arrived from outside the system. The counterpart of
/// <see cref="Notifications.OutboxMessage"/>: the outbox makes sure something leaves exactly once,
/// the inbox makes sure something that arrives is acted on exactly once.
/// <para>
/// External senders retry. A payment gateway that does not get a prompt acknowledgement will call
/// back again, and the tax authority will resend a filing result. Recording the sender's own
/// identifier and refusing a second row for it is what stops one payment being credited twice.
/// </para>
/// </summary>
public sealed class InboxMessage : AggregateRoot
{
    public IntegrationSource Source { get; private set; }

    /// <summary>
    /// The identifier the sender gave this message. Unique per source, and the whole point of the
    /// table: a repeat delivery is recognised by this value, not by comparing payloads.
    /// </summary>
    public string ExternalMessageId { get; private set; } = default!;

    /// <summary>What kind of message this is, in the sender's own vocabulary.</summary>
    public string MessageType { get; private set; } = default!;

    /// <summary>The message exactly as it arrived, normally JSON, kept for dispute and replay.</summary>
    public string Payload { get; private set; } = default!;

    public InboxMessageStatus Status { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public DateTimeOffset? NextAttemptOn { get; private set; }

    public int AttemptCount { get; private set; }
    public string? Error { get; private set; }
    public string? CorrelationId { get; private set; }

    /// <summary>What the message turned out to be about, filled in once it has been understood.</summary>
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }

    private InboxMessage() { }

    public InboxMessage(IntegrationSource source, string externalMessageId, string messageType, string payload, string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalMessageId, nameof(externalMessageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType, nameof(messageType));
        ArgumentException.ThrowIfNullOrWhiteSpace(payload, nameof(payload));

        Source = source;
        ExternalMessageId = externalMessageId.Trim();
        MessageType = messageType.Trim();
        Payload = payload;
        CorrelationId = correlationId;
        Status = InboxMessageStatus.Received;
        ReceivedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsProcessing() => Status = InboxMessageStatus.Processing;

    public void MarkAsProcessed(string? referenceType = null, string? referenceId = null)
    {
        Status = InboxMessageStatus.Processed;
        ProcessedOn = DateTimeOffset.UtcNow;
        Error = null;
        NextAttemptOn = null;
        SetReference(referenceType, referenceId);
    }

    /// <summary>
    /// Records a failed attempt and backs off exponentially, giving up after
    /// <paramref name="maxAttempts"/>. An abandoned message is left for a human to look at rather
    /// than dropped, because the sender believes it was delivered.
    /// </summary>
    public void MarkAsFailed(string error, int maxAttempts = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error, nameof(error));
        AttemptCount++;
        Error = error;

        if (AttemptCount >= maxAttempts)
        {
            Status = InboxMessageStatus.Abandoned;
            NextAttemptOn = null;
            return;
        }

        Status = InboxMessageStatus.Received;
        NextAttemptOn = DateTimeOffset.UtcNow.AddSeconds(Math.Pow(2, AttemptCount) * 10);
    }

    /// <summary>
    /// For a message that is understood but deliberately not acted on - a notification about
    /// something this installation does not track. It is answered, not retried.
    /// </summary>
    public void Discard(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = InboxMessageStatus.Discarded;
        ProcessedOn = DateTimeOffset.UtcNow;
        Error = reason.Trim();
        NextAttemptOn = null;
    }

    public void SetReference(string? referenceType, string? referenceId)
    {
        ReferenceType = string.IsNullOrWhiteSpace(referenceType) ? ReferenceType : referenceType.Trim();
        ReferenceId = string.IsNullOrWhiteSpace(referenceId) ? ReferenceId : referenceId.Trim();
    }

    /// <summary>Whether the work this message asks for has already been done.</summary>
    public bool IsSettled => Status is InboxMessageStatus.Processed or InboxMessageStatus.Discarded;
}
