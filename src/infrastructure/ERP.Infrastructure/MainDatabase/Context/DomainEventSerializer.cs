using System.Text.Json;
using System.Text.Json.Serialization;

namespace ERP.Infrastructure.MainDatabase.Context;

/// <summary>
/// Turns a domain event into an outbox payload and back.
/// <para>
/// The stored type name is the short name, not the assembly qualified one. An assembly qualified
/// name pins rows to an assembly version and a namespace, so moving an event between folders or
/// renaming the project would orphan every message already written. The short name is resolved
/// against the domain assembly at run time instead, and a name that no longer resolves is a loud
/// failure rather than a silently skipped message.
/// </para>
/// </summary>
public static class DomainEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null
    };

    private static readonly Lazy<IReadOnlyDictionary<string, Type>> KnownEvents = new(() =>
        typeof(IDomainEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IDomainEvent)))
            .ToDictionary(t => t.Name, t => t, StringComparer.Ordinal));

    public static string TypeNameOf(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent, nameof(domainEvent));
        return domainEvent.GetType().Name;
    }

    public static string Serialize(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent, nameof(domainEvent));
        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Options);
    }

    public static IDomainEvent Deserialize(string messageType, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType, nameof(messageType));
        ArgumentException.ThrowIfNullOrWhiteSpace(payload, nameof(payload));

        if (!KnownEvents.Value.TryGetValue(messageType, out var clrType))
            throw new InvalidOperationException($"No domain event type named {messageType} exists in the domain assembly.");

        return JsonSerializer.Deserialize(payload, clrType, Options) as IDomainEvent
            ?? throw new InvalidOperationException($"The payload for {messageType} deserialized to null.");
    }

    /// <summary>Every event name the domain defines, for start-up checks and diagnostics.</summary>
    public static IReadOnlyCollection<string> KnownEventNames => [.. KnownEvents.Value.Keys];
}
