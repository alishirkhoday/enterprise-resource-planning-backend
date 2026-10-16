using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// Subsidiary accounts - تفصیلی. Who or what a posting is against, kept separate from the
/// account that says what kind of posting it is.
/// </summary>
public interface IDetailAccountRepository : IRepository<DetailAccount>
{
    Task<DetailAccount?> GetByCodeAsync(Guid groupId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// The subsidiary account that stands for a particular record, such as the one carrying a
    /// given customer balance. This is how a customer is connected to the ledger.
    /// </summary>
    Task<DetailAccount?> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DetailAccount>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>The next free code in a group, used when a new subsidiary account is opened.</summary>
    Task<int> GetCountByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
}
