using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using LedgerLineRow = ERP.Domain.Repositories.Modules.FinanceManagement.LedgerLineRow;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class DocumentRepository(IMainDbContext context) : Repository<Document>(context), IDocumentRepository
{
    public async Task<Document?> GetByNumberAsync(string number, Guid financialYearId, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(
            d => d.Number == number && d.FinancialYearId == financialYearId,
            cancellationToken);
    }

    public async Task<Document?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(d => d.Items).ThenInclude(i => i.Account)
            .Include(d => d.Items).ThenInclude(i => i.DetailAccount1)
            .Include(d => d.Items).ThenInclude(i => i.DetailAccount2)
            .Include(d => d.Items).ThenInclude(i => i.DetailAccount3)
            .Include(d => d.FinancialYear)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Document?> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(
            d => d.ReferenceType == referenceType && d.ReferenceId == referenceId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByPeriodAsync(Guid accountingPeriodId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(d => d.AccountingPeriodId == accountingPeriodId)
            .OrderBy(d => d.FinancialEventDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetDraftsAsync(Guid financialYearId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(d => d.FinancialYearId == financialYearId && d.Status == DocumentStatus.Draft)
            .OrderBy(d => d.FinancialEventDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetLastNumberAsync(Guid financialYearId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(d => d.FinancialYearId == financialYearId && d.Number != null)
            .OrderByDescending(d => d.Number)
            .Select(d => d.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Summed by the database. A trial balance over a year touches every line of every entry, and
    /// loading those into memory to add them up would be the slowest part of the report.
    /// </summary>
    public async Task<(decimal Debtor, decimal Creditor)> GetAccountTurnoverAsync(
        Guid accountId,
        Guid financialYearId,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentItems
            .AsNoTracking()
            .Where(i => i.AccountId == accountId
                        && i.Document.FinancialYearId == financialYearId
                        && i.Document.Status == DocumentStatus.Definite);

        if (from.HasValue)
        {
            var fromMoment = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(i => i.Document.FinancialEventDateTime >= fromMoment);
        }

        if (to.HasValue)
        {
            var toMoment = new DateTimeOffset(to.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            query = query.Where(i => i.Document.FinancialEventDateTime <= toMoment);
        }

        var totals = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Debtor = g.Sum(i => i.Debtor),
                Creditor = g.Sum(i => i.Creditor)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return (totals?.Debtor ?? 0m, totals?.Creditor ?? 0m);
    }

    /// <summary>
    /// Only posted lines. A draft is somebody's intention, and a ledger that shows intentions is
    /// a ledger nobody can reconcile against a bank statement.
    /// </summary>
    public async Task<IReadOnlyList<LedgerLineRow>> GetLedgerLinesAsync(
        Guid accountId,
        DateOnly from,
        DateOnly to,
        Guid? detailAccountId = null,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var fromMoment = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toMoment = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        return await _context.DocumentItems
            .AsNoTracking()
            .Where(i => i.AccountId == accountId
                        && i.Document.Status == DocumentStatus.Definite
                        && i.Document.FinancialEventDateTime >= fromMoment
                        && i.Document.FinancialEventDateTime <= toMoment
                        && (branchId == null || i.Document.BranchId == branchId)
                        && (detailAccountId == null
                            || i.DetailAccount1Id == detailAccountId
                            || i.DetailAccount2Id == detailAccountId
                            || i.DetailAccount3Id == detailAccountId))
            .OrderBy(i => i.Document.FinancialEventDateTime)
            .ThenBy(i => i.Document.Number)
            .ThenBy(i => i.Priority)
            .Select(i => new LedgerLineRow(
                i.DocumentId,
                i.Document.Number,
                i.Document.FinancialEventDateTime,
                i.Document.Title,
                i.Description,
                i.Debtor,
                i.Creditor))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Grouped and summed in the database, one round trip for the whole trial balance. Asking per
    /// account would be one query for every account in the chart, which on a real chart is
    /// hundreds.
    /// </summary>
    public async Task<IReadOnlyList<AccountTurnoverRow>> GetTurnoverByAccountAsync(
        DateOnly? from,
        DateOnly to,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var toMoment = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        var fromMoment = from.HasValue
            ? new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : (DateTimeOffset?)null;

        return await _context.DocumentItems
            .AsNoTracking()
            .Where(i => i.Document.Status == DocumentStatus.Definite
                        && i.Document.FinancialEventDateTime <= toMoment
                        && (fromMoment == null || i.Document.FinancialEventDateTime >= fromMoment)
                        && (branchId == null || i.Document.BranchId == branchId))
            .GroupBy(i => new { i.AccountId, i.Account.Code, i.Account.Name })
            .Select(g => new AccountTurnoverRow(
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                g.Sum(i => i.Debtor),
                g.Sum(i => i.Creditor)))
            .OrderBy(r => r.AccountCode)
            .ToListAsync(cancellationToken);
    }
}
