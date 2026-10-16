using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Integration;
using ERP.Domain.Repositories.Integration;

namespace ERP.Infrastructure.MainDatabase.Repositories.Integration;

public class InboxMessageRepository(IMainDbContext context) : Repository<InboxMessage>(context), IInboxMessageRepository
{
    /// <summary>
    /// Deliberately tracked and unfiltered on status. The caller is deciding whether to process a
    /// delivery, and a row that came back read-only would be useless to them the moment they find
    /// one that still needs finishing.
    /// </summary>
    public async Task<InboxMessage?> FindByExternalIdAsync(IntegrationSource source, string externalMessageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalMessageId, nameof(externalMessageId));

        return await _entity.FirstOrDefaultAsync(
            m => m.Source == source && m.ExternalMessageId == externalMessageId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<InboxMessage>> GetPendingBatchAsync(int batchSize, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize, nameof(batchSize));

        return await _entity
            .Where(m => m.Status == InboxMessageStatus.Received
                        && (m.NextAttemptOn == null || m.NextAttemptOn <= asOf))
            .OrderBy(m => m.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InboxMessage>> GetAbandonedAsync(IntegrationSource? source = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(m => m.Status == InboxMessageStatus.Abandoned
                        && (source == null || m.Source == source))
            .OrderByDescending(m => m.ReceivedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InboxMessage>> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(m => m.ReferenceType == referenceType && m.ReferenceId == referenceId)
            .OrderBy(m => m.ReceivedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Soft deletes, like everything else here. An abandoned message is never purged: it is the
    /// only record that the sender believes something was delivered which was never acted on.
    /// </summary>
    public async Task<int> PurgeSettledBeforeAsync(DateTimeOffset before, CancellationToken cancellationToken = default)
    {
        var settled = new[] { InboxMessageStatus.Processed, InboxMessageStatus.Discarded };

        var stale = await _entity
            .Where(m => settled.Contains(m.Status) && m.ProcessedOn < before)
            .ToListAsync(cancellationToken);

        foreach (var message in stale)
        {
            message.Delete();
        }

        return stale.Count;
    }
}
