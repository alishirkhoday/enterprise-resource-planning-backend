namespace ERP.Application.Common.Interfaces.Events;

/// <summary>
/// Drains the outbox: reads the facts that were recorded alongside a business change and hands
/// them to their handlers.
/// <para>
/// Called on a schedule rather than at the end of a request. Publishing inside the transaction
/// would let a handler observe a change that then rolled back, and publishing after the commit
/// without a record would lose the event if the process died in between - the outbox row is what
/// removes both possibilities.
/// </para>
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>Processes one batch and returns how many messages were dispatched successfully.</summary>
    Task<int> ProcessPendingAsync(int batchSize = 50, CancellationToken cancellationToken = default);
}
