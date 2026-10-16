using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// Money in and out, and what each payment was applied to - تسویه حساب.
/// </summary>
public interface ISettlementRepository : IRepository<Settlement>
{
    Task<Settlement?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    /// <summary>A settlement with its allocations, which is the only form in which it balances.</summary>
    Task<Settlement?> GetWithAllocationsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Settlement>> GetByPartyAsync(Guid partyDetailAccountId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Money taken from a party that is not yet applied to anything, which is what an advance is.
    /// </summary>
    Task<IReadOnlyList<Settlement>> GetUnallocatedAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default);

    /// <summary>What has been settled against one open item, from whichever receipts touched it.</summary>
    Task<decimal> GetAllocatedTotalForReceivableAsync(Guid receivableId, CancellationToken cancellationToken = default);
}
