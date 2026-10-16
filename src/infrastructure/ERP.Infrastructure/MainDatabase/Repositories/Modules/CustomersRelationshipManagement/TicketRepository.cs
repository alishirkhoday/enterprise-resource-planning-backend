using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.CustomersRelationshipManagement;

public class TicketRepository(IMainDbContext context) : Repository<Ticket>(context), ITicketRepository
{
    private static readonly TicketStatus[] OpenStatuses =
    [
        TicketStatus.Open,
        TicketStatus.InProgress,
        TicketStatus.AwaitingCustomer
    ];

    public async Task<Ticket?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(t => t.Number == number, cancellationToken);
    }

    public async Task<Ticket?> GetWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(t => t.Customer)
            .Include(t => t.Subject)
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(t => t.Customer)
            .Where(t => OpenStatuses.Contains(t.Status)
                        && (assignedToHumanId == null || t.AssignedToHumanId == assignedToHumanId))
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreationDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreationDateTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// A breach is either a first response that never came in time, or a resolution that is late.
    /// Both are expressed against the stored due moments so the database can do the comparison.
    /// </summary>
    public async Task<IReadOnlyList<Ticket>> GetBreachingServiceLevelAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(t => t.Customer)
            .Where(t => OpenStatuses.Contains(t.Status)
                        && ((t.ResponseDueAt != null && t.FirstRespondedAt == null && t.ResponseDueAt < asOf)
                            || (t.ResolutionDueAt != null && t.ResolvedAt == null && t.ResolutionDueAt < asOf)))
            .OrderBy(t => t.ResolutionDueAt)
            .ToListAsync(cancellationToken);
    }
}
