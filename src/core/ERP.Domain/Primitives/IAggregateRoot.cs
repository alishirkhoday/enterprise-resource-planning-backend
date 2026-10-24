namespace ERP.Domain.Primitives;

/// <summary>Marks an independently persisted consistency boundary.</summary>
public interface IAggregateRoot<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; }

    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>Called by infrastructure once the events have been dispatched.</summary>
    void ClearDomainEvents();
}
