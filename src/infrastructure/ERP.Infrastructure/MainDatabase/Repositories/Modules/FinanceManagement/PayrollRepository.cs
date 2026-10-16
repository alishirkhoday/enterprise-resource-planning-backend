using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class PayrollRepository(IMainDbContext context) : Repository<Payroll>(context), IPayrollRepository
{
    public async Task<Payroll?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(p => p.Items)
            .Include(p => p.Rewards)
            .Include(p => p.Deductions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payroll?> GetByHumanAndPeriodAsync(Guid humanId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(p => p.Items)
            .FirstOrDefaultAsync(
                p => p.HumanId == humanId && p.StartDate == startDate && p.EndDate == endDate,
                cancellationToken);
    }

    /// <summary>
    /// Any overlap counts, not just an exact match. A period that partly covers one already paid
    /// would pay the same days twice.
    /// </summary>
    public async Task<bool> ExistsForPeriodAsync(Guid humanId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(
            p => p.HumanId == humanId && p.StartDate <= endDate && p.EndDate >= startDate,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Payroll>> GetByPeriodAsync(DateOnly startDate, DateOnly endDate, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.StartDate == startDate
                        && p.EndDate == endDate
                        && (branchId == null || p.BranchId == branchId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payroll>> GetUnpaidAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.Status == PayrollStatus.Definite
                        && p.PaidOn == null
                        && (branchId == null || p.BranchId == branchId))
            .OrderBy(p => p.EndDate)
            .ToListAsync(cancellationToken);
    }
}
