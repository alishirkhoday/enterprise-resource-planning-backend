using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

namespace ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;
/// <summary>
/// Service visits - سفارش خدمات. The work itself, as against the contract that entitles the
/// customer to it.
/// </summary>
public interface IServiceOrderRepository : IRepository<ServiceOrder>
{
    Task<ServiceOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    Task<ServiceOrder?> GetWithPartsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>An engineer's work list: what is open, most urgent and oldest first.</summary>
    Task<IReadOnlyList<ServiceOrder>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceOrder>> GetScheduledBetweenAsync(DateOnly from, DateOnly to, Guid? assignedToHumanId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceOrder>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Open orders that have missed a promised response or resolution.</summary>
    Task<IReadOnlyList<ServiceOrder>> GetBreachingServiceLevelAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finished chargeable work that nobody has billed. Service revenue leaks here more reliably
    /// than anywhere else in an ERP, because the engineer's job ends when the machine runs.
    /// </summary>
    Task<IReadOnlyList<ServiceOrder>> GetAwaitingInvoiceAsync(CancellationToken cancellationToken = default);

    /// <summary>Orders held up waiting for a part, oldest first.</summary>
    Task<IReadOnlyList<ServiceOrder>> GetAwaitingPartsAsync(CancellationToken cancellationToken = default);
}
