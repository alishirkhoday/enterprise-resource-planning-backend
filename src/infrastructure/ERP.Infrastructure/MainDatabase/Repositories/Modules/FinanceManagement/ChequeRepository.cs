using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class ChequeRepository(IMainDbContext context) : Repository<Cheque>(context), IChequeRepository
{
    /// <summary>States in which a cheque is still waiting to be settled one way or the other.</summary>
    private static readonly ChequeStatus[] OpenStatuses =
    [
        ChequeStatus.InHand,
        ChequeStatus.InCollection,
        ChequeStatus.Issued
    ];

    public async Task<Cheque?> GetBySayadNumberAsync(string sayadNumber, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(c => c.SayadNumber == sayadNumber, cancellationToken);
    }

    public async Task<Cheque?> GetWithOperationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Operations)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Cheque>> GetByStatusAsync(ChequeType type, ChequeStatus status, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.Type == type && c.Status == status)
            .OrderBy(c => c.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cheque>> GetDueBetweenAsync(ChequeType type, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.Type == type
                        && c.DueDate >= from
                        && c.DueDate <= to
                        && OpenStatuses.Contains(c.Status))
            .OrderBy(c => c.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cheque>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.DueDate < asOf && OpenStatuses.Contains(c.Status))
            .OrderBy(c => c.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cheque>> GetOpenByPartyAsync(Guid detailAccountId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.DetailAccountId == detailAccountId && OpenStatuses.Contains(c.Status))
            .OrderBy(c => c.DueDate)
            .ToListAsync(cancellationToken);
    }
}
