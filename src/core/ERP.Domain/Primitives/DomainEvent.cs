namespace ERP.Domain.Primitives;

/// <summary>
/// Base record for domain events. Carries the identity and the moment the fact occurred,
/// so handlers can be replayed and ordered independently of when they run.
/// <para>
/// Both properties are settable on initialisation rather than read only. An event that is written
/// to an outbox and dispatched minutes later has to come back from storage as the same fact, and a
/// get-only property would be silently replaced with a fresh identity and a fresh timestamp on the
/// way back - which is exactly the pair of values a consumer uses to recognise a repeat.
/// </para>
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
