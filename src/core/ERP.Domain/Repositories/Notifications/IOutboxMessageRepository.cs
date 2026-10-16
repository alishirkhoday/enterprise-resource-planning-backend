using ERP.Domain.Entities.Notifications;

namespace ERP.Domain.Repositories.Notifications;
/// <summary>
/// The outbox. Domain events are written here in the same transaction as the change that
/// raised them, then drained on a schedule, so a fact is never published for a change that
/// turned out not to commit.
/// </summary>
public interface IOutboxMessageRepository : IRepository<OutboxMessage>
{
    /// <summary>
    /// The next batch to send: pending, and either never attempted or past the moment their
    /// backoff allows another try. Taken oldest first so nothing starves.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingBatchAsync(int batchSize, DateTimeOffset asOf, CancellationToken cancellationToken = default);

    /// <summary>Messages that ran out of attempts, which somebody needs to look at.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetAbandonedAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes messages that were delivered long enough ago to stop keeping.</summary>
    Task<int> PurgeProcessedBeforeAsync(DateTimeOffset before, CancellationToken cancellationToken = default);
}
