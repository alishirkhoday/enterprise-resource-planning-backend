using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
/// <summary>
/// Customers. The party every sale, invoice and receivable hangs off.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<Customer?> GetByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default);

    Task<Customer?> GetByEconomicCodeAsync(string economicCode, CancellationToken cancellationToken = default);

    Task<Customer?> GetWithAddressesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Customer?> GetWithContactsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Customers that may actually be sold to today.</summary>
    Task<IReadOnlyList<Customer>> GetSellableAsync(Guid? customerGroupId = null, CancellationToken cancellationToken = default);

    Task<bool> IsExistenceByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Customers whose outstanding balance has passed the limit they were granted. Sales reads
    /// this before promising anything further.
    /// </summary>
    Task<IReadOnlyList<Customer>> GetOverCreditLimitAsync(CancellationToken cancellationToken = default);
}
