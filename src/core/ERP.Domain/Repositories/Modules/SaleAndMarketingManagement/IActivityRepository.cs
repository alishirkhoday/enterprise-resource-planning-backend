using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
/// <summary>
/// Calls, meetings and follow-ups - فعالیت. Ordered by when they are due, because an activity
/// list read in any other order is a list nobody works from.
/// </summary>
public interface IActivityRepository : IRepository<Activity>
{
    /// <summary>
    /// What is on somebody's plate. Ordered by when it is due, because an activity list read in
    /// any other order is a list nobody works from.
    /// </summary>
    Task<IReadOnlyList<Activity>> GetOpenForAsync(Guid assignedToHumanId, CancellationToken cancellationToken = default);

    /// <summary>Everything logged against one lead, opportunity or customer.</summary>
    Task<IReadOnlyList<Activity>> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activities past their due time and still open. A follow up that quietly slipped is how a
    /// deal goes cold, so it is queried rather than worked out in memory.
    /// </summary>
    Task<IReadOnlyList<Activity>> GetOverdueAsync(DateTimeOffset asOf, Guid? assignedToHumanId = null, CancellationToken cancellationToken = default);

    /// <summary>Activities due in a window, which is what a reminder sweep reads.</summary>
    Task<IReadOnlyList<Activity>> GetDueBetweenAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
