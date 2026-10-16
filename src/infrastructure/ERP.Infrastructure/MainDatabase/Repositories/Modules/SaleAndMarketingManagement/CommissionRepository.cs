using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.SaleAndMarketingManagement;

public class CommissionRepository(IMainDbContext context) : Repository<Commission>(context), ICommissionRepository
{
    /// <summary>
    /// A reversed commission does not block a fresh one: reversing is how a mistake is undone,
    /// and the correction has to be allowed to take its place.
    /// </summary>
    public async Task<Commission?> GetBySourceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await _entity
            .FirstOrDefaultAsync(
                c => c.ReferenceType == referenceType
                     && c.ReferenceId == referenceId
                     && c.Status != CommissionStatus.Reversed,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Commission>> GetBySalesRepAsync(Guid salesRepHumanId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.SalesRepHumanId == salesRepHumanId
                        && (from == null || c.EarnedOn >= from)
                        && (to == null || c.EarnedOn <= to))
            .OrderByDescending(c => c.EarnedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Commission>> GetPayableAsync(Guid? salesRepHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.Status == CommissionStatus.Approved
                        && (salesRepHumanId == null || c.SalesRepHumanId == salesRepHumanId))
            .OrderBy(c => c.EarnedOn)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reversed rows are excluded, so a corrected commission does not still count toward what
    /// somebody earned.
    /// </summary>
    public async Task<decimal> GetEarnedTotalAsync(Guid salesRepHumanId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.SalesRepHumanId == salesRepHumanId
                        && c.Status != CommissionStatus.Reversed
                        && c.EarnedOn >= from
                        && c.EarnedOn <= to)
            .SumAsync(c => (decimal?)c.Amount.Amount, cancellationToken) ?? 0m;
    }
}
