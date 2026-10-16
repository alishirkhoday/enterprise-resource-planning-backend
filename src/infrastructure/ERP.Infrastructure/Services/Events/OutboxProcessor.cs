using ERP.Application.Common.Interfaces.DbContext;
using ERP.Application.Common.Interfaces.Events;
using ERP.Domain.Repositories.Notifications;
using ERP.Infrastructure.MainDatabase.Context;

namespace ERP.Infrastructure.Services.Events;

/// <summary>
/// Reads the facts recorded alongside business changes and hands them to their handlers.
/// </summary>
public sealed class OutboxProcessor(
    IOutboxMessageRepository messages,
    IDomainEventDispatcher dispatcher,
    IMainDbContext context) : IOutboxProcessor
{
    public async Task<int> ProcessPendingAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize, nameof(batchSize));

        var batch = await messages.GetPendingBatchAsync(batchSize, DateTimeOffset.UtcNow, cancellationToken);
        if (batch.Count == 0)
            return 0;

        var dispatched = 0;

        foreach (var message in batch)
        {
            // Each message is judged on its own. One poisonous payload backing off on its own
            // schedule must not hold up the queue behind it.
            try
            {
                message.MarkAsProcessing();

                var domainEvent = DomainEventSerializer.Deserialize(message.MessageType, message.Payload);
                await dispatcher.DispatchAsync(domainEvent, cancellationToken);

                message.MarkAsProcessed();
                dispatched++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.MarkAsFailed(exception.Message);
            }
        }

        // Saved once for the whole batch. Every message in it has reached a terminal state or been
        // scheduled for another attempt, so a failure here costs a redelivery, never a lost fact.
        await context.SaveChanges(cancellationToken);

        return dispatched;
    }
}
