namespace ERP.Domain.Entities.Users;

/// <summary>
/// A refresh token, stored as a hash. The raw token is shown to the client once and never
/// kept, so a leaked database cannot be replayed as a login.
/// </summary>
public sealed class UserToken : BaseEntity
{
    public User User { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public string TokenHash { get; } = default!;
    public DateTimeOffset ExpireDateTime { get; } = default!;

    private UserToken() { }

    public UserToken(User user, string tokenHash)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        User = user;
        UserId = user.Id;
        TokenHash = tokenHash;
        ExpireDateTime = DateTimeOffset.UtcNow;
    }
}
