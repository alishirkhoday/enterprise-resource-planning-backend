namespace ERP.Domain.Entities.Auditing;

/// <summary>
/// One field that changed as part of an <see cref="AuditLog"/> entry.
/// </summary>
public sealed class AuditLogChange : BaseEntity
{
    public AuditLog AuditLog { get; private set; } = default!;
    public Guid AuditLogId { get; private set; }

    public string FieldName { get; private set; } = default!;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }

    private AuditLogChange() { }

    public AuditLogChange(AuditLog auditLog, string fieldName, string? oldValue, string? newValue)
    {
        ArgumentNullException.ThrowIfNull(auditLog, nameof(auditLog));
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
        AuditLog = auditLog;
        AuditLogId = auditLog.Id;
        FieldName = fieldName.Trim();
        OldValue = oldValue;
        NewValue = newValue;
    }
}
