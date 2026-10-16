using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.SaleAndMarketingManagement;

public class ActivityRepository(IMainDbContext context) : Repository<Activity>(context), IActivityRepository
{
    private static readonly ActivityStatus[] OpenStatuses =
    [
        ActivityStatus.Planned,
        ActivityStatus.InProgress
    ];

    public async Task<IReadOnlyList<Activity>> GetOpenForAsync(Guid assignedToHumanId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => a.AssignedToHumanId == assignedToHumanId && OpenStatuses.Contains(a.Status))
            .OrderBy(a => a.DueAt ?? DateTimeOffset.MaxValue)
            .ThenByDescending(a => a.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Activity>> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => a.ReferenceType == referenceType && a.ReferenceId == referenceId)
            .OrderByDescending(a => a.DueAt ?? a.CreationDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Activity>> GetOverdueAsync(DateTimeOffset asOf, Guid? assignedToHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => OpenStatuses.Contains(a.Status)
                        && a.DueAt != null
                        && a.DueAt < asOf
                        && (assignedToHumanId == null || a.AssignedToHumanId == assignedToHumanId))
            .OrderBy(a => a.DueAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Activity>> GetDueBetweenAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => OpenStatuses.Contains(a.Status) && a.DueAt != null && a.DueAt >= from && a.DueAt <= to)
            .OrderBy(a => a.DueAt)
            .ToListAsync(cancellationToken);
    }
}
