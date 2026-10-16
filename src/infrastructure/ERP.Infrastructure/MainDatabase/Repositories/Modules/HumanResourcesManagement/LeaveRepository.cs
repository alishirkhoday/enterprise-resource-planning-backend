using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.HumanResourcesManagement;

public class LeaveRepository(IMainDbContext context) : Repository<HumanRequest>(context), ILeaveRepository
{
    private static readonly HumanRequestStatus[] LiveStatuses =
    [
        HumanRequestStatus.Pending,
        HumanRequestStatus.Approved
    ];

    public async Task<HumanRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(r => r.Human)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
    }

    public Task AddRequestAsync(HumanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _entity.Add(request);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cancelled and rejected requests are left out: those days were given back, so they cannot
    /// clash with anything.
    /// </summary>
    public async Task<IReadOnlyList<HumanRequest>> GetOverlappingRequestsAsync(Guid humanId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(r => r.HumanId == humanId
                        && LiveStatuses.Contains(r.Status)
                        && r.StartDate <= to
                        && r.EndDate >= from)
            .OrderBy(r => r.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HumanRequest>> GetPendingAsync(Guid? approverHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(r => r.Human)
            .Where(r => r.Status == HumanRequestStatus.Pending
                        && (approverHumanId == null || r.Human.ManagerId == approverHumanId))
            .OrderBy(r => r.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Tracked, because the caller is about to reserve or consume days against it.</summary>
    public async Task<LeaveEntitlement?> GetEntitlementAsync(Guid humanId, HumanRequestLeaveType leaveType, int year, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveEntitlements
            .FirstOrDefaultAsync(
                e => e.HumanId == humanId && e.LeaveType == leaveType && e.Year == year,
                cancellationToken);
    }

    public Task AddEntitlementAsync(LeaveEntitlement entitlement, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entitlement, nameof(entitlement));
        _context.LeaveEntitlements.Add(entitlement);
        return Task.CompletedTask;
    }
}
