using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Repositories.Modules.HumanResourcesManagement;

/// <summary>Leave requests and the balances they are checked against - مرخصی.</summary>
public interface ILeaveRepository : IRepository<HumanRequest>
{
    Task<HumanRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task AddRequestAsync(HumanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leave already asked for that overlaps a window, so a second request for the same days is
    /// caught before it reaches an approver.
    /// </summary>
    Task<IReadOnlyList<HumanRequest>> GetOverlappingRequestsAsync(Guid humanId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// What is waiting on one approver, or on everybody. A request nobody is looking at is how
    /// somebody ends up taking leave they were never granted.
    /// </summary>
    Task<IReadOnlyList<HumanRequest>> GetPendingAsync(Guid? approverHumanId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The balance for a kind of leave in a year, which a request has to be checked against
    /// before it can be granted.
    /// </summary>
    Task<LeaveEntitlement?> GetEntitlementAsync(Guid humanId, HumanRequestLeaveType leaveType, int year, CancellationToken cancellationToken = default);

    Task AddEntitlementAsync(LeaveEntitlement entitlement, CancellationToken cancellationToken = default);
}
