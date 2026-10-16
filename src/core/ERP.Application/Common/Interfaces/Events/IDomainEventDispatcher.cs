using ERP.Domain.Primitives;

namespace ERP.Application.Common.Interfaces.Events;

/// <summary>
/// Publishes a domain event to whoever is listening. The concrete type is only known at run time -
/// an outbox row deserializes into <see cref="IDomainEvent"/> - so the dispatcher is what closes
/// the generic wrapper over the real type before handing it to the mediator.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
