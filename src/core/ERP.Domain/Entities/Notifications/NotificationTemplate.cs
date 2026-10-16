namespace ERP.Domain.Entities.Notifications;

/// <summary>
/// The reusable body of a message. Keeping wording out of code means it can be corrected
/// without a deployment, and translated per language.
/// </summary>
public sealed class NotificationTemplate : BaseEntity
{
    /// <summary>Identifies the event that triggers it, for example Order.Confirmed.</summary>
    public string Key { get; private set; } = default!;

    public NotificationChannel Channel { get; private set; }
    public string? Subject { get; private set; }
    public string Body { get; private set; } = default!;

    /// <summary>ISO language tag, for example fa-IR.</summary>
    public string Language { get; private set; } = default!;

    /// <summary>Identifier of the provider side template, used by SMS gateways that require pre-registration.</summary>
    public string? ProviderTemplateId { get; private set; }

    public bool IsActive { get; private set; }

    private NotificationTemplate() { }

    public NotificationTemplate(string key, NotificationChannel channel, string body, string language = "fa-IR")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(body, nameof(body));
        ArgumentException.ThrowIfNullOrWhiteSpace(language, nameof(language));
        Key = key.Trim();
        Channel = channel;
        Body = body;
        Language = language.Trim();
        IsActive = true;
    }

    public void Update(string? subject, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body, nameof(body));
        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        Body = body;
    }

    public void SetProviderTemplateId(string? providerTemplateId) =>
        ProviderTemplateId = string.IsNullOrWhiteSpace(providerTemplateId) ? null : providerTemplateId.Trim();

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
