namespace ERP.Domain.Entities.Users;

/// <summary>
/// What a user did, kept for security review. Unlike an audit log this is about the actor,
/// not about a changed record.
/// </summary>
public sealed class UserActivityHistory : BaseEntity
{
    public User User { get; private set; } = default!;
    public Guid UserId { get; private set; }

    public UserActivityType Type { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }
    public bool IsSuccessful { get; private set; }

    public string? Description { get; private set; }
    public string? InternetProtocolAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceName { get; private set; }

    /// <summary>What the activity was about, when it targeted a specific record.</summary>
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }

    private UserActivityHistory() { }

    public UserActivityHistory(User user, UserActivityType type, bool isSuccessful = true)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        User = user;
        UserId = user.Id;
        Type = type;
        IsSuccessful = isSuccessful;
        OccurredOn = DateTimeOffset.UtcNow;
    }

    internal void SetRequestContext(string? internetProtocolAddress, string? userAgent, string? deviceName)
    {
        InternetProtocolAddress = internetProtocolAddress;
        UserAgent = userAgent;
        DeviceName = deviceName;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    internal void SetReference(string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
    }
}
