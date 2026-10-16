using ERP.Domain.Entities.Security;
using ERP.Domain.Entities.Users;
using System.Linq.Expressions;

namespace ERP.Domain.Repositories.Security;
/// <summary>
/// What a user is allowed to do. Read on nearly every request, so the shape of these queries
/// matters more than most.
/// </summary>
public interface IPermissionRepository : IRepository<Permission>
{
    /// <summary>
    /// The permissions a user currently holds, reached through their effective role assignments.
    /// </summary>
    Task<List<T>> GetPermissionsByUserAsync<T>(
        User user,
        Expression<Func<Permission, T>> expression,
        CancellationToken cancellationToken = default);

    Task<Permission?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> IsExistenceByKeyAsync(string key, CancellationToken cancellationToken = default);
}
