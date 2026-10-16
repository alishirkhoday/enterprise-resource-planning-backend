using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Common;
using ERP.Domain.Repositories.Common;

namespace ERP.Infrastructure.MainDatabase.Repositories.Common;

public class SystemSettingRepository(IMainDbContext context) : Repository<SystemSetting>(context), ISystemSettingRepository
{
    /// <summary>
    /// The branch specific value wins where one exists, otherwise the organization wide one. The
    /// ordering is what expresses that precedence in a single query.
    /// </summary>
    public async Task<SystemSetting?> GetByKeyAsync(string key, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        return await Query
            .Where(s => s.Key == key && (s.BranchId == null || s.BranchId == branchId))
            .OrderByDescending(s => s.BranchId != null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> GetValueAsync(string key, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var setting = await GetByKeyAsync(key, branchId, cancellationToken);
        return setting?.Value;
    }

    public async Task<IReadOnlyList<SystemSetting>> GetByPrefixAsync(string keyPrefix, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix, nameof(keyPrefix));

        return await Query
            .Where(s => s.Key.StartsWith(keyPrefix) && (s.BranchId == null || s.BranchId == branchId))
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);
    }
}
