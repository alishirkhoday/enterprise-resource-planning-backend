using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class PayableRepository(IMainDbContext context) : Repository<Payable>(context), IPayableRepository
{
    private static readonly PayableStatus[] OpenStatuses =
    [
        PayableStatus.Open,
        PayableStatus.PartiallySettled
    ];

    public async Task<IReadOnlyList<Payable>> GetOpenByPartyAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Where(p => p.PartyDetailAccountId == partyDetailAccountId && OpenStatuses.Contains(p.Status))
            .OrderBy(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payable?> GetBySourceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(
            p => p.ReferenceType == referenceType && p.ReferenceId == referenceId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Payable>> GetOverdueAsync(DateOnly asOf, Guid? partyDetailAccountId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.DueDate < asOf
                        && OpenStatuses.Contains(p.Status)
                        && (partyDetailAccountId == null || p.PartyDetailAccountId == partyDetailAccountId))
            .OrderBy(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Anything on hold is left out. A payment run must not pay an invoice somebody has flagged as
    /// disputed, so the filter belongs here rather than in each caller.
    /// </summary>
    public async Task<IReadOnlyList<Payable>> GetDueForPaymentAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Where(p => p.DueDate >= from
                        && p.DueDate <= to
                        && !p.IsOnHold
                        && OpenStatuses.Contains(p.Status))
            .OrderBy(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetOutstandingTotalAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.PartyDetailAccountId == partyDetailAccountId && OpenStatuses.Contains(p.Status))
            .SumAsync(p => p.OriginalAmount.Amount - p.SettledAmount.Amount, cancellationToken);
    }
}
