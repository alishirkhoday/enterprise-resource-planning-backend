namespace ERP.Domain.Entities.Notifications;

/// <summary>
/// Where an outbox row has got to. The whole point of the outbox is that a fact is written
/// with the change that caused it and published afterwards, so this tracks the second half.
/// </summary>
public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Processed = 2,
    Abandoned = 3
}
