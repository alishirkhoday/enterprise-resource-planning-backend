namespace ERP.Domain.Primitives;

/// <summary>
/// The contract a repository is written against: something with an identity that records the facts
/// it has raised. <see cref="BaseEntity"/> is currently the only implementation, so the constraint
/// admits every entity; it stays because it is the place to narrow the set if child entities are
/// ever given a base of their own.
/// </summary>
public interface IAggregateRoot<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; }

    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>Called by infrastructure once the events have been dispatched.</summary>
    void ClearDomainEvents();
}
