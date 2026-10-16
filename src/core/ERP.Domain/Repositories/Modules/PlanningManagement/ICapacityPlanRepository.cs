using ERP.Domain.Entities.Modules.PlanningManagement;

namespace ERP.Domain.Repositories.Modules.PlanningManagement;
/// <summary>
/// Capacity plans - برنامه ظرفیت. Whether the work centres can actually do what was planned.
/// </summary>
public interface ICapacityPlanRepository : IRepository<CapacityPlan>
{
    Task<CapacityPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<CapacityPlan?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CapacityPlan?> GetLatestCompletedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Buckets where more work is planned than there are effective hours to do it, worst overload
    /// first. This is the list that decides whether a material plan is actually deliverable.
    /// </summary>
    Task<IReadOnlyList<CapacityPlanLine>> GetOverloadedLinesAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>The load profile for one work centre across a plan, in date order.</summary>
    Task<IReadOnlyList<CapacityPlanLine>> GetLoadForWorkCenterAsync(Guid planId, Guid workCenterId, CancellationToken cancellationToken = default);
}
