using ERP.Domain.Entities.Users;

namespace ERP.Domain.Repositories.Users;
/// <summary>
/// Active sign-ins. What lets somebody be signed out of one device without being signed out
/// everywhere, and what an administrator reads after a password is stolen.
/// </summary>
public interface IUserSessionRepository : IRepository<UserSession>
{
    /// <summary>
    /// Sessions a user currently has open. Showing someone where they are signed in, and letting
    /// them end a session they do not recognise, both read this.
    /// </summary>
    Task<IReadOnlyList<UserSession>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserSession?> GetActiveByUserAndDeviceAsync(Guid userId, string internetProtocolAddress, string? deviceName, CancellationToken cancellationToken = default);

    /// <summary>Sessions whose expiry has passed but which were never closed.</summary>
    Task<IReadOnlyList<UserSession>> GetExpiredAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);

    Task<int> CountActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
