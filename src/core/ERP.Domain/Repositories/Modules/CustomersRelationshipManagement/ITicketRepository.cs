using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
/// <summary>
/// Support tickets. The queries here are mostly about time: what is open, what is late, what
/// breached the response somebody was promised.
/// </summary>
public interface ITicketRepository : IRepository<Ticket>
{
    Task<Ticket?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    Task<Ticket?> GetWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tickets that have passed the response or resolution time they were promised. Support is
    /// measured on these, so they are queried rather than computed in memory.
    /// </summary>
    Task<IReadOnlyList<Ticket>> GetBreachingServiceLevelAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);
}
