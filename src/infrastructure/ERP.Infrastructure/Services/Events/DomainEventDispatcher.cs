using System.Collections.Concurrent;
using ERP.Application.Common.Events;
using ERP.Application.Common.Interfaces.Events;
using MediatR;

namespace ERP.Infrastructure.Services.Events;

/// <summary>
/// Closes <see cref="DomainEventNotification{TDomainEvent}"/> over the event's real type and hands
/// it to the mediator.
/// <para>
/// The indirection exists because the concrete type is only known at run time: an outbox row comes
/// back as <see cref="IDomainEvent"/>, and publishing it as that would reach no handler, since a
/// handler subscribes to the specific fact rather than to the idea of one.
/// </para>
/// </summary>
public sealed class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    /// <summary>
    /// One constructed generic type per event type. Building it costs reflection; the outbox
    /// processes the same handful of event types over and over, so it is built once.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Type> NotificationTypes = new();

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent, nameof(domainEvent));

        var notificationType = NotificationTypes.GetOrAdd(
            domainEvent.GetType(),
            eventType => typeof(DomainEventNotification<>).MakeGenericType(eventType));

        var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

        await publisher.Publish(notification, cancellationToken);
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents, nameof(domainEvents));

        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}
