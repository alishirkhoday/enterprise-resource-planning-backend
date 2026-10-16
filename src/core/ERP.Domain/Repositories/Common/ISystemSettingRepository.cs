using ERP.Domain.Entities.Common;

namespace ERP.Domain.Repositories.Common;
/// <summary>
/// Configuration that belongs to the business rather than to the deployment, such as the default
/// currency or a rounding policy. Things an administrator changes without a release.
/// </summary>
public interface ISystemSettingRepository : IRepository<SystemSetting>
{
    /// <summary>
    /// A setting, preferring the one set for the branch and falling back to the organization wide
    /// value when the branch has not overridden it.
    /// </summary>
    Task<SystemSetting?> GetByKeyAsync(string key, Guid? branchId = null, CancellationToken cancellationToken = default);

    Task<string?> GetValueAsync(string key, Guid? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>Loads a whole section in one query, for code that needs several related settings.</summary>
    Task<IReadOnlyList<SystemSetting>> GetByPrefixAsync(string keyPrefix, Guid? branchId = null, CancellationToken cancellationToken = default);
}
