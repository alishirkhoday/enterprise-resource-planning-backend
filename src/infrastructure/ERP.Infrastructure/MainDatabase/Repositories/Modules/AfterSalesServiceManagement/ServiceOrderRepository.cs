using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.AfterSalesServiceManagement;

public class ServiceOrderRepository(IMainDbContext context) : Repository<ServiceOrder>(context), IServiceOrderRepository
{
    private static readonly ServiceOrderStatus[] OpenStatuses =
    [
        ServiceOrderStatus.Open,
        ServiceOrderStatus.Assigned,
        ServiceOrderStatus.Scheduled,
        ServiceOrderStatus.InProgress,
        ServiceOrderStatus.AwaitingParts
    ];

    public async Task<ServiceOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(o => o.Number == number, cancellationToken);
    }

    public async Task<ServiceOrder?> GetWithPartsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(o => o.Customer)
            .Include(o => o.Parts).ThenInclude(p => p.Item)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceOrder>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Customer)
            .Where(o => OpenStatuses.Contains(o.Status)
                        && (assignedToHumanId == null || o.AssignedToHumanId == assignedToHumanId))
            .OrderByDescending(o => o.Priority)
            .ThenBy(o => o.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceOrder>> GetScheduledBetweenAsync(DateOnly from, DateOnly to, Guid? assignedToHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Customer)
            .Where(o => o.ScheduledOn != null
                        && o.ScheduledOn >= from
                        && o.ScheduledOn <= to
                        && o.Status != ServiceOrderStatus.Cancelled
                        && (assignedToHumanId == null || o.AssignedToHumanId == assignedToHumanId))
            .OrderBy(o => o.ScheduledOn)
            .ThenByDescending(o => o.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceOrder>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// A breach is either a first response that never came in time or a resolution that is late.
    /// Both are expressed against the stored due moments so the database does the comparison.
    /// </summary>
    public async Task<IReadOnlyList<ServiceOrder>> GetBreachingServiceLevelAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Customer)
            .Where(o => OpenStatuses.Contains(o.Status)
                        && ((o.ResponseDueAt != null && o.RespondedAt == null && o.ResponseDueAt < asOf)
                            || (o.ResolutionDueAt != null && o.CompletedOn == null && o.ResolutionDueAt < asOf)))
            .OrderBy(o => o.ResolutionDueAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Only chargeable work can reach an invoice, so warranty and contract visits are excluded
    /// here rather than left for the caller to filter out and get wrong.
    /// </summary>
    public async Task<IReadOnlyList<ServiceOrder>> GetAwaitingInvoiceAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Customer)
            .Include(o => o.Parts)
            .Where(o => o.Status == ServiceOrderStatus.Completed
                        && o.Type == ServiceOrderType.Chargeable
                        && o.InvoiceId == null)
            .OrderBy(o => o.CompletedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceOrder>> GetAwaitingPartsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Customer)
            .Include(o => o.Parts).ThenInclude(p => p.Item)
            .Where(o => o.Status == ServiceOrderStatus.AwaitingParts)
            .OrderBy(o => o.RequestedAt)
            .ToListAsync(cancellationToken);
    }
}
