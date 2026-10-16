using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

namespace ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;
/// <summary>
/// Support and maintenance agreements - قرارداد خدمات. What a customer is entitled to, and
/// until when.
/// </summary>
public interface IServiceContractRepository : IRepository<ServiceContract>
{
    Task<ServiceContract?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    Task<ServiceContract?> GetWithCoverageAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The active contract covering a particular piece of equipment on a date. It is what decides
    /// whether an incoming service call is already paid for and how fast somebody has to answer.
    /// <para>
    /// A contract naming the serial number wins over one that covers the whole item, because the
    /// specific agreement is the one that was negotiated for that unit.
    /// </para>
    /// </summary>
    Task<ServiceContract?> GetCoveringAsync(Guid customerId, Guid itemId, Guid? serialNumberId, DateOnly onDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceContract>> GetActiveForCustomerAsync(Guid customerId, DateOnly onDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceContract>> GetDueForRenewalAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>Active contracts whose included visits are used up, so further calls are chargeable.</summary>
    Task<IReadOnlyList<ServiceContract>> GetWithExhaustedVisitsAsync(CancellationToken cancellationToken = default);
}
