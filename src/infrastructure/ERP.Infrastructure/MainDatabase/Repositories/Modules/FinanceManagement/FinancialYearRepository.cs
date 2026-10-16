using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class FinancialYearRepository(IMainDbContext context) : Repository<FinancialYear>(context), IFinancialYearRepository
{
    public async Task<FinancialYear?> GetByDateAsync(DateOnly date, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(y => y.Periods)
            .FirstOrDefaultAsync(
                y => y.StartDate <= date
                     && y.EndDate >= date
                     && (branchId == null || y.BranchId == branchId || y.BranchId == null),
                cancellationToken);
    }

    public async Task<FinancialYear?> GetOpenYearAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(y => y.Periods)
            .Where(y => y.Status == FinancialYearStatus.Open
                        && (branchId == null || y.BranchId == branchId || y.BranchId == null))
            .OrderByDescending(y => y.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FinancialYear?> GetWithPeriodsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(y => y.Periods)
            .FirstOrDefaultAsync(y => y.Id == id, cancellationToken);
    }

    public async Task<AccountingPeriod?> GetPeriodByDateAsync(DateOnly date, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var year = await GetByDateAsync(date, branchId, cancellationToken);
        return year?.PeriodOn(date);
    }

    public async Task<bool> HasOverlappingYearAsync(DateOnly startDate, DateOnly endDate, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        // Two ranges overlap when each starts before the other ends.
        return await Query.AnyAsync(
            y => (excludeId == null || y.Id != excludeId)
                 && y.StartDate <= endDate
                 && y.EndDate >= startDate,
            cancellationToken);
    }
}
