using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class AccountRepository(IMainDbContext context) : Repository<Account>(context), IAccountRepository
{
    public async Task<bool> IsExistenceByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await Query.AnyAsync(a => a.Name == name, cancellationToken);
    }

    public async Task<int> GetCountAccountsByAccountTypeAndAccountGroupAsync(AccountType type, AccountGroup group, CancellationToken cancellationToken)
    {
        return await Query.CountAsync(a => a.Type == type && a.Group == group, cancellationToken);
    }

    public async Task<int> GetCountChildAccountsByAccountIdAsync(Guid parentId, CancellationToken cancellationToken)
    {
        return await Query.CountAsync(a => a.ParentId == parentId, cancellationToken);
    }

    public async Task<Account?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
    }

    public async Task<bool> IsExistenceByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(a => a.Code == code, cancellationToken);
    }

    public async Task<Account?> GetWithDetailGroupsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(a => a.DetailGroups)
            .ThenInclude(dg => dg.DetailAccountGroup)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => a.ParentId == parentId)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetPostableAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => a.IsFinal && !a.IsLocked)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetByTypeAsync(AccountType type, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => a.Type == type)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
    }
}
