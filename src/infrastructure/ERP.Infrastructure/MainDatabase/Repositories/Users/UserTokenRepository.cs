using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Users;

namespace ERP.Infrastructure.MainDatabase.Repositories.Users;

public class UserTokenRepository(IMainDbContext context) : Repository<UserToken>(context), IUserTokenRepository
{
    public async Task<UserToken?> GetByUserAndTokenAsync(User user, string token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        return await _entity.FirstOrDefaultAsync(
            t => t.UserId == user.Id && t.TokenHash == token,
            cancellationToken);
    }

    public async Task<IReadOnlyList<UserToken>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _entity
            .Where(t => t.UserId == userId && t.ExpireDateTime > now)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserToken>> GetExpiredAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Where(t => t.ExpireDateTime <= asOf)
            .ToListAsync(cancellationToken);
    }
}
