using ERP.Domain.Primitives;

namespace ERP.Application.Common.Events;

/// <summary>
/// Carries a domain event onto the mediator without the domain having to know the mediator exists.
/// <para>
/// This wrapper is the whole reason <see cref="IDomainEvent"/> is an empty interface: the domain
/// records a fact, and the adapting to whatever publishes it happens here, one layer out. Handlers
/// subscribe as <c>INotificationHandler&lt;DomainEventNotification&lt;OrderPlaced&gt;&gt;</c>.
/// </para>
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
