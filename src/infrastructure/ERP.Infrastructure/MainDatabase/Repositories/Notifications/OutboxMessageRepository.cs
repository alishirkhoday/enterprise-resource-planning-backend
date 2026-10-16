using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Notifications;
using ERP.Domain.Repositories.Notifications;

namespace ERP.Infrastructure.MainDatabase.Repositories.Notifications;

public class OutboxMessageRepository(IMainDbContext context) : Repository<OutboxMessage>(context), IOutboxMessageRepository
{
    /// <summary>
    /// Tracked, because the dispatcher is about to mark each one processed or failed. Ordered by
    /// when the fact happened so nothing waits behind a newer message.
    /// </summary>
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingBatchAsync(int batchSize, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize, nameof(batchSize));

        return await _entity
            .Where(m => m.Status == OutboxMessageStatus.Pending
                        && (m.NextAttemptOn == null || m.NextAttemptOn <= asOf))
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetAbandonedAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(m => m.Status == OutboxMessageStatus.Abandoned)
            .OrderBy(m => m.OccurredOn)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Soft deletes the batch. Even here the rows are kept rather than dropped, so the table stays
    /// consistent with how everything else in the system is removed.
    /// </summary>
    public async Task<int> PurgeProcessedBeforeAsync(DateTimeOffset before, CancellationToken cancellationToken = default)
    {
        var stale = await _entity
            .Where(m => m.Status == OutboxMessageStatus.Processed && m.ProcessedOn < before)
            .ToListAsync(cancellationToken);

        foreach (var message in stale)
        {
            message.Delete();
        }

        return stale.Count;
    }
}
