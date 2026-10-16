namespace ERP.Domain.Entities.Auditing;

/// <summary>
/// An immutable record of one change to one entity. Financial software has to be able to answer
/// who changed what and when, long after the change itself.
/// </summary>
public sealed class AuditLog : BaseEntity
{
    public string EntityName { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public AuditAction Action { get; private set; }

    public Guid? UserId { get; private set; }
    public string? Username { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }

    public string? InternetProtocolAddress { get; private set; }
    public string? UserAgent { get; private set; }

    /// <summary>Correlates every log written while handling one request.</summary>
    public string? CorrelationId { get; private set; }

    private readonly List<AuditLogChange> _changes = [];
    public IReadOnlyList<AuditLogChange> Changes => _changes;

    private AuditLog() { }

    public AuditLog(string entityName, string entityId, AuditAction action, Guid? userId = null, string? username = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName, nameof(entityName));
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId, nameof(entityId));
        EntityName = entityName.Trim();
        EntityId = entityId.Trim();
        Action = action;
        UserId = userId;
        Username = username;
        OccurredOn = DateTimeOffset.UtcNow;
    }

    public void SetRequestContext(string? internetProtocolAddress, string? userAgent, string? correlationId)
    {
        InternetProtocolAddress = internetProtocolAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
    }

    public void AddChange(AuditLogChange change)
    {
        ArgumentNullException.ThrowIfNull(change, nameof(change));
        _changes.Add(change);
    }
}
