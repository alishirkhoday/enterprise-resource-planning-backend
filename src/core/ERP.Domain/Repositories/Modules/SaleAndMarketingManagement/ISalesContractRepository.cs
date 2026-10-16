using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
/// <summary>
/// Framework agreements with customers - قرارداد فروش. What a sale draws its terms from when
/// one applies.
/// </summary>
public interface ISalesContractRepository : IRepository<SalesContract>
{
    Task<SalesContract?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    /// <summary>
    /// The contract that governs a sale to this customer on this date. A sale made under a
    /// contract draws its prices and terms from there rather than from the standard list.
    /// </summary>
    Task<SalesContract?> GetEffectiveForCustomerAsync(Guid customerId, DateOnly onDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Contracts coming up for renewal inside their notice period. Missing one is how a contract
    /// auto renews on terms nobody meant to agree to again.
    /// </summary>
    Task<IReadOnlyList<SalesContract>> GetDueForRenewalAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesContract>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
