using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// The chart of accounts - کدینگ حسابداری. A tree, so most questions about it are about a
/// branch rather than a row.
/// </summary>
public interface IAccountRepository : IRepository<Account>
{
    Task<bool> IsExistenceByNameAsync(string name, CancellationToken cancellationToken);
    Task<int> GetCountAccountsByAccountTypeAndAccountGroupAsync(AccountType type, AccountGroup group, CancellationToken cancellationToken);
    Task<int> GetCountChildAccountsByAccountIdAsync(Guid parentId, CancellationToken cancellationToken);

    Task<Account?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsExistenceByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// An account together with the subsidiary groups it declares, which a journal line needs in
    /// order to validate what it is posted against.
    /// </summary>
    Task<Account?> GetWithDetailGroupsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The direct children of an account, in code order.</summary>
    Task<IReadOnlyList<Account>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The accounts a journal line may actually be posted to: final, unlocked and not deleted.
    /// </summary>
    Task<IReadOnlyList<Account>> GetPostableAccountsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> GetByTypeAsync(AccountType type, CancellationToken cancellationToken = default);
}
