using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class SettlementRepository(IMainDbContext context) : Repository<Settlement>(context), ISettlementRepository
{
    public async Task<Settlement?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(s => s.Number == number, cancellationToken);
    }

    public async Task<Settlement?> GetWithAllocationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(s => s.Allocations)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Settlement>> GetByPartyAsync(Guid partyDetailAccountId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(s => s.PartyDetailAccountId == partyDetailAccountId
                        && (from == null || s.SettlementDate >= from)
                        && (to == null || s.SettlementDate <= to))
            .OrderByDescending(s => s.SettlementDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Money received that has not been applied to anything. Comparing the settlement amount with
    /// the sum of its allocations is how an advance is recognised.
    /// </summary>
    public async Task<IReadOnlyList<Settlement>> GetUnallocatedAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(s => s.Allocations)
            .Where(s => s.PartyDetailAccountId == partyDetailAccountId
                        && s.Status == SettlementStatus.Confirmed
                        && s.Allocations.Sum(a => a.Amount.Amount) < s.Amount.Amount)
            .OrderBy(s => s.SettlementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetAllocatedTotalForReceivableAsync(Guid receivableId, CancellationToken cancellationToken = default)
    {
        return await _context.SettlementAllocations
            .AsNoTracking()
            .Where(a => a.ReceivableId == receivableId && a.Settlement.Status == SettlementStatus.Confirmed)
            .SumAsync(a => a.Amount.Amount, cancellationToken);
    }
}
