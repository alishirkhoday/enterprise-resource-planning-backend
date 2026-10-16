namespace ERP.Domain.Entities.Users;

/// <summary>
/// Represents a user session. Useful for global logout, device/session management and audit.
/// </summary>
public sealed class UserSession : BaseEntity
{
    public User User { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public UserSessionStatus Status { get; private set; }
    public string InternetProtocolAddress { get; private set; } = default!;
    public string? UserAgent { get; private set; }
    public string? DeviceName { get; private set; }
    public string? OperatingSystem { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private UserSession() { }

    public UserSession(User user, string ipAddress, string? userAgent = null, string? deviceName = null, string? operatingSystem = null, DateTimeOffset? expiresAt = null)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        ArgumentException.ThrowIfNullOrEmpty(ipAddress, nameof(ipAddress));
        User = user;
        UserId = user.Id;
        InternetProtocolAddress = ipAddress.Trim();
        UserAgent = userAgent;
        DeviceName = deviceName;
        OperatingSystem = operatingSystem;
        StartedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        Status = UserSessionStatus.Active;
    }

    public void Revoke()
    {
        if (Status != UserSessionStatus.Active)
        {
            return;
        }
        Status = UserSessionStatus.Revoked;
        RevokedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkExpired()
    {
        if (Status != UserSessionStatus.Active)
        {
            return;
        }
        Status = UserSessionStatus.Expired;
        Touch();
    }
}
