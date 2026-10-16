using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class BudgetRepository(IMainDbContext context) : Repository<Budget>(context), IBudgetRepository
{
    public async Task<Budget?> GetByTitleAsync(string title, Guid financialYearId, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(
            b => b.Title == title && b.FinancialYearId == financialYearId,
            cancellationToken);
    }

    public async Task<Budget?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(b => b.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Budget>> GetByFinancialYearAsync(Guid financialYearId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(b => b.FinancialYearId == financialYearId)
            .OrderByDescending(b => b.Version).ThenBy(b => b.Title)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// A cost centre of null on the line means it governs the account across every cost centre, so
    /// a line scoped to the asked-for centre wins and the unscoped one is the fallback.
    /// </summary>
    public async Task<BudgetLine?> GetLineForAsync(Guid financialYearId, Guid accountId, short periodNumber, Guid? costCenterId = null, CancellationToken cancellationToken = default)
    {
        return await _context.BudgetLines
            .AsNoTracking()
            .Where(l => l.Budget.FinancialYearId == financialYearId
                        && l.AccountId == accountId
                        && l.PeriodNumber == periodNumber
                        && (l.CostCenterId == null || l.CostCenterId == costCenterId))
            .OrderByDescending(l => l.CostCenterId != null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// The comparison is written against the stored amounts rather than the entity's
    /// <c>IsOverspent</c>, which has no column behind it and would pull every line into memory.
    /// </summary>
    public async Task<IReadOnlyList<BudgetLine>> GetOverspentLinesAsync(Guid? financialYearId = null, CancellationToken cancellationToken = default)
    {
        return await _context.BudgetLines
            .AsNoTracking()
            .Include(l => l.Account)
            .Where(l => l.ActualAmount.Amount > l.PlannedAmount.Amount
                        && (financialYearId == null || l.Budget.FinancialYearId == financialYearId))
            .OrderByDescending(l => l.ActualAmount.Amount - l.PlannedAmount.Amount)
            .ToListAsync(cancellationToken);
    }
}
