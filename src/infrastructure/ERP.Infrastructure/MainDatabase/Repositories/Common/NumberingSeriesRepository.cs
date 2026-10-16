using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Common;
using ERP.Domain.Repositories.Common;

namespace ERP.Infrastructure.MainDatabase.Repositories.Common;

public class NumberingSeriesRepository(IMainDbContext context) : Repository<NumberingSeries>(context), INumberingSeriesRepository
{
    /// <summary>
    /// Returned tracked, so the concurrency stamp is loaded with it. When two requests consume the
    /// same counter, the second save is rejected rather than quietly issuing a duplicate number,
    /// and the caller retries against the value the winner committed.
    /// </summary>
    public async Task<NumberingSeries?> GetForUpdateAsync(string key, Guid? branchId = null, Guid? financialYearId = null, CancellationToken cancellationToken = default)
    {
        return await FindAsync(_entity, key, branchId, financialYearId, cancellationToken);
    }

    public async Task<NumberingSeries?> GetByKeyAsync(string key, Guid? branchId = null, Guid? financialYearId = null, CancellationToken cancellationToken = default)
    {
        return await FindAsync(Query, key, branchId, financialYearId, cancellationToken);
    }

    /// <summary>
    /// Prefers the most specific series. A series scoped to this branch and year beats one scoped
    /// to the branch alone, which in turn beats the organization wide fallback.
    /// </summary>
    private static async Task<NumberingSeries?> FindAsync(
        IQueryable<NumberingSeries> source,
        string key,
        Guid? branchId,
        Guid? financialYearId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        return await source
            .Where(s => s.Key == key
                        && s.IsActive
                        && (s.BranchId == null || s.BranchId == branchId)
                        && (s.FinancialYearId == null || s.FinancialYearId == financialYearId))
            .OrderByDescending(s => s.BranchId != null)
            .ThenByDescending(s => s.FinancialYearId != null)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
