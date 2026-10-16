using ERP.Domain.Entities.Security;

namespace ERP.Domain.Repositories.Security;
/// <summary>
/// Roles and the permissions attached to them.
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByTitleAsync(string title, CancellationToken cancellationToken = default);

    /// <summary>A role with the permissions it grants, which is what an authorization check needs.</summary>
    Task<Role?> GetWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsExistenceByTitleAsync(string title, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
