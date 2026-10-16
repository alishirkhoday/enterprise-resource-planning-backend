using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Security;
using ERP.Domain.Repositories.Security;

namespace ERP.Infrastructure.MainDatabase.Repositories.Security;

public class RoleRepository(IMainDbContext context) : Repository<Role>(context), IRoleRepository
{
    public async Task<Role?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        var normalized = Normalization.NormalizeUsername(title);
        return await _entity.FirstOrDefaultAsync(r => r.NormalizedTitle == normalized, cancellationToken);
    }

    public async Task<Role?> GetWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <summary>
    /// Roles the user holds right now. An assignment that has expired grants nothing, so the
    /// expiry is part of the query rather than something the caller has to remember.
    /// </summary>
    public async Task<IReadOnlyList<Role>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId
                         && (ur.ExpiresOn == null || ur.ExpiresOn > now)
                         && ur.Role.IsActive)
            .Select(ur => ur.Role)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistenceByTitleAsync(string title, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = Normalization.NormalizeUsername(title);
        return await Query.AnyAsync(
            r => r.NormalizedTitle == normalized && (excludeId == null || r.Id != excludeId),
            cancellationToken);
    }
}
