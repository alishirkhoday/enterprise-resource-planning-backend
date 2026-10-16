using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Security;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Security;
using System.Linq.Expressions;

namespace ERP.Infrastructure.MainDatabase.Repositories.Users
{
    public class UserPermissionRepository(IMainDbContext context) : Repository<Permission>(context), IPermissionRepository
    {
        /// <summary>
        /// Walks user to role to permission. Permissions are never held directly by a user, so an
        /// expired or removed role assignment takes its permissions with it.
        /// </summary>
        public async Task<List<T>> GetPermissionsByUserAsync<T>(
            User user,
            Expression<Func<Permission, T>> expression,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            var now = DateTimeOffset.UtcNow;

            var result = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id
                             && (ur.ExpiresOn == null || ur.ExpiresOn > now)
                             && ur.Role.IsActive)
                .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission))
                .Distinct()
                .Select(expression)
                .ToListAsync(cancellationToken);

            return result;
        }

        public async Task<Permission?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
            return await _entity.FirstOrDefaultAsync(p => p.Key == key, cancellationToken);
        }

        public async Task<bool> IsExistenceByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
            return await _entity.AnyAsync(p => p.Key == key, cancellationToken);
        }
    }
}
