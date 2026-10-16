using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Users;

namespace ERP.Infrastructure.MainDatabase.Repositories.Users;

public class UserSessionRepository(IMainDbContext context) : Repository<UserSession>(context), IUserSessionRepository
{
    public async Task<IReadOnlyList<UserSession>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await Query
            .Where(s => s.UserId == userId
                        && s.Status == UserSessionStatus.Active
                        && s.RevokedAt == null
                        && (s.ExpiresAt == null || s.ExpiresAt > now))
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserSession?> GetActiveByUserAndDeviceAsync(Guid userId, string internetProtocolAddress, string? deviceName, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _entity
            .Where(s => s.UserId == userId
                        && s.Status == UserSessionStatus.Active
                        && s.RevokedAt == null
                        && (s.ExpiresAt == null || s.ExpiresAt > now)
                        && s.InternetProtocolAddress == internetProtocolAddress
                        && s.DeviceName == deviceName)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSession>> GetExpiredAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Where(s => s.Status == UserSessionStatus.Active
                        && s.ExpiresAt != null
                        && s.ExpiresAt <= asOf)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await Query.CountAsync(
            s => s.UserId == userId
                 && s.Status == UserSessionStatus.Active
                 && s.RevokedAt == null
                 && (s.ExpiresAt == null || s.ExpiresAt > now),
            cancellationToken);
    }
}
