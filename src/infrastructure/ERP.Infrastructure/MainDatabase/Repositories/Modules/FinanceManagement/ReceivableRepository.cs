using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class ReceivableRepository(IMainDbContext context) : Repository<Receivable>(context), IReceivableRepository
{
    private static readonly ReceivableStatus[] OpenStatuses =
    [
        ReceivableStatus.Open,
        ReceivableStatus.PartiallySettled
    ];

    public async Task<IReadOnlyList<Receivable>> GetOpenByPartyAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Where(r => r.PartyDetailAccountId == partyDetailAccountId && OpenStatuses.Contains(r.Status))
            .OrderBy(r => r.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Receivable?> GetBySourceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(
            r => r.ReferenceType == referenceType && r.ReferenceId == referenceId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Receivable>> GetOverdueAsync(DateOnly asOf, Guid? partyDetailAccountId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(r => r.DueDate < asOf
                        && OpenStatuses.Contains(r.Status)
                        && (partyDetailAccountId == null || r.PartyDetailAccountId == partyDetailAccountId))
            .OrderBy(r => r.DueDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The subtraction is done in the query rather than by reading the computed property, because
    /// OutstandingAmount is derived in memory and the database cannot see it.
    /// </summary>
    public async Task<decimal> GetOutstandingTotalAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(r => r.PartyDetailAccountId == partyDetailAccountId && OpenStatuses.Contains(r.Status))
            .SumAsync(r => r.OriginalAmount.Amount - r.SettledAmount.Amount, cancellationToken);
    }

    public async Task<IReadOnlyList<ReceivableAgeingRow>> GetAgeingRowsAsync(Guid? partyDetailAccountId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(r => OpenStatuses.Contains(r.Status)
                        && (partyDetailAccountId == null || r.PartyDetailAccountId == partyDetailAccountId))
            .OrderBy(r => r.DueDate)
            .Select(r => new ReceivableAgeingRow(
                r.Id,
                r.PartyDetailAccountId,
                r.DueDate,
                r.OriginalAmount.Amount - r.SettledAmount.Amount,
                r.OriginalAmount.CurrencyCode))
            .ToListAsync(cancellationToken);
    }
}
