using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class DetailAccountRepository(IMainDbContext context) : Repository<DetailAccount>(context), IDetailAccountRepository
{
    public async Task<DetailAccount?> GetByCodeAsync(Guid groupId, string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(d => d.GroupId == groupId && d.Code == code, cancellationToken);
    }

    public async Task<DetailAccount?> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(d => d.Group)
            .FirstOrDefaultAsync(d => d.Group.ReferenceType == referenceType && d.ReferenceId == referenceId, cancellationToken);
    }

    public async Task<IReadOnlyList<DetailAccount>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(d => d.GroupId == groupId && d.IsActive)
            .OrderBy(d => d.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await Query.CountAsync(d => d.GroupId == groupId, cancellationToken);
    }
}
