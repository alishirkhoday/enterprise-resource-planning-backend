namespace ERP.Domain.Primitives;

/// <summary>
/// A fact that has already happened, recorded by the entity it happened to.
/// <para>
/// Tied to no framework: dispatching is somebody else's problem, so the domain never has to know
/// that a mediator, a bus or an outbox exists.
/// </para>
/// <para>
/// The two members are domain concerns, not plumbing. An event that is stored and dispatched later
/// has to be recognisable as the same fact when it comes back, and a consumer that sees it twice
/// has only the identity to tell a repeat from a genuine second occurrence.
/// </para>
/// </summary>
public interface IDomainEvent
{
    /// <summary>Identifies this occurrence, so a repeat delivery can be recognised as one.</summary>
    Guid EventId { get; }

    /// <summary>When the fact happened, which is not when a handler gets to hear about it.</summary>
    DateTimeOffset OccurredOn { get; }
}
