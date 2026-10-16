using ERP.Domain.Entities.Users;

namespace ERP.Domain.Repositories.Users;
/// <summary>
/// Refresh and verification tokens. Short-lived rows whose whole value is that they can be
/// revoked before they expire.
/// </summary>
public interface IUserTokenRepository : IRepository<UserToken>
{
    Task<UserToken?> GetByUserAndTokenAsync(User user, string token, CancellationToken cancellationToken = default);

    /// <summary>Tokens a user still holds, so signing out everywhere can revoke them all.</summary>
    Task<IReadOnlyList<UserToken>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tokens whose expiry has passed. They no longer authenticate anyone, so a maintenance job
    /// clears them out rather than letting the table grow without limit.
    /// </summary>
    Task<IReadOnlyList<UserToken>> GetExpiredAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);
}
