using ERP.Domain.Entities.Integration;

namespace ERP.Domain.Repositories.Integration;
/// <summary>
/// Messages that arrived from outside - سامانه مؤدیان, a bank, a marketplace. Stored before
/// they are processed so a delivery is never lost to a failure on our side.
/// </summary>
public interface IInboxMessageRepository : IRepository<InboxMessage>
{
    /// <summary>
    /// The message a sender delivered under this identifier, if it has been seen before. The
    /// caller decides what to do about it; this only answers whether it is new.
    /// <para>
    /// Deliberately not named <c>ExistsAsync</c>: a repeat delivery of something still being
    /// processed is a different situation from a repeat of something already settled, and the
    /// caller needs the row to tell them apart.
    /// </para>
    /// </summary>
    Task<InboxMessage?> FindByExternalIdAsync(IntegrationSource source, string externalMessageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Messages waiting to be acted on, oldest first, including failures whose back-off has
    /// elapsed. Tracked, because the caller is about to mark each one processed or failed.
    /// </summary>
    Task<IReadOnlyList<InboxMessage>> GetPendingBatchAsync(int batchSize, DateTimeOffset asOf, CancellationToken cancellationToken = default);

    /// <summary>Messages that ran out of attempts and need a person, newest first.</summary>
    Task<IReadOnlyList<InboxMessage>> GetAbandonedAsync(IntegrationSource? source = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InboxMessage>> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    /// <summary>Soft deletes settled messages older than the given moment.</summary>
    Task<int> PurgeSettledBeforeAsync(DateTimeOffset before, CancellationToken cancellationToken = default);
}
