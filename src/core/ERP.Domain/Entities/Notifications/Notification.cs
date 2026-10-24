namespace ERP.Domain.Entities.Notifications;

/// <summary>
/// A message addressed to one recipient. The rendered text is stored rather than re-rendered,
/// so what was actually sent stays auditable even after the template changes.
/// </summary>
public sealed class Notification : AggregateRoot
{
    public Guid? TemplateId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationPriority Priority { get; private set; }
    public NotificationStatus Status { get; private set; }

    /// <summary>The user the message belongs to, when there is one.</summary>
    public Guid? RecipientUserId { get; private set; }

    /// <summary>Phone number, email address or device token, depending on the channel.</summary>
    public string RecipientAddress { get; private set; } = default!;

    public string? Subject { get; private set; }
    public string Body { get; private set; } = default!;

    /// <summary>What the message is about, so clicking it can navigate somewhere.</summary>
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }

    public DateTimeOffset? ScheduledFor { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? FailureReason { get; private set; }

    private Notification() { }

    public Notification(NotificationChannel channel, string recipientAddress, string body, NotificationPriority priority = NotificationPriority.Normal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientAddress, nameof(recipientAddress));
        ArgumentException.ThrowIfNullOrWhiteSpace(body, nameof(body));
        Channel = channel;
        RecipientAddress = recipientAddress.Trim();
        Body = body;
        Priority = priority;
        Status = NotificationStatus.Pending;
    }

    public void SetTemplate(Guid templateId) => TemplateId = templateId == Guid.Empty ? null : templateId;

    public void SetSubject(string? subject) => Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();

    public void SetRecipientUser(Guid userId) => RecipientUserId = userId == Guid.Empty ? null : userId;

    public void SetReference(string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
    }

    public void ScheduleFor(DateTimeOffset when)
    {
        if (Status != NotificationStatus.Pending)
            throw new InvalidOperationException("Only a pending notification can be scheduled.");
        ScheduledFor = when;
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
        FailureReason = null;
    }

    public void MarkAsFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        AttemptCount++;
        Status = NotificationStatus.Failed;
        FailureReason = reason;
    }

    public void MarkAsRead()
    {
        if (Status != NotificationStatus.Sent)
            throw new InvalidOperationException("Only a sent notification can be marked as read.");
        Status = NotificationStatus.Read;
        ReadAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is NotificationStatus.Sent or NotificationStatus.Read)
            throw new InvalidOperationException("A delivered notification cannot be cancelled.");
        Status = NotificationStatus.Cancelled;
    }
}
