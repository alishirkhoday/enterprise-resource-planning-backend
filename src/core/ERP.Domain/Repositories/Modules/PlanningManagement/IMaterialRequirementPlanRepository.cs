using ERP.Domain.Entities.Modules.PlanningManagement;

namespace ERP.Domain.Repositories.Modules.PlanningManagement;
/// <summary>
/// Planning runs and their suggestions - MRP. A run is a snapshot of a position that has since
/// moved, which is why its status decides whether it is still worth acting on.
/// </summary>
public interface IMaterialRequirementPlanRepository : IRepository<MaterialRequirementPlan>
{
    Task<MaterialRequirementPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<MaterialRequirementPlan?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The newest completed run, which is the one whose suggestions are worth acting on.</summary>
    Task<MaterialRequirementPlan?> GetLatestCompletedAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The buyer's and planner's work list: lines that suggest something and have not been turned
    /// into an order yet, soonest release date first.
    /// </summary>
    Task<IReadOnlyList<MaterialRequirementPlanLine>> GetPendingReleaseLinesAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lines whose release date has already gone by. The shortfall can no longer be avoided by
    /// ordering on time, so somebody has to renegotiate a date rather than plan around it.
    /// </summary>
    Task<IReadOnlyList<MaterialRequirementPlanLine>> GetPastDueLinesAsync(Guid planId, DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>Every suggestion this run made for one item, across the horizon.</summary>
    Task<IReadOnlyList<MaterialRequirementPlanLine>> GetLinesForItemAsync(Guid planId, Guid itemId, CancellationToken cancellationToken = default);
}
