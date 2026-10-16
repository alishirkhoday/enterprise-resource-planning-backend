using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.HumanResourcesManagement;

public class AttendanceRepository(IMainDbContext context) : Repository<HumanAttendanceRecord>(context), IAttendanceRepository
{
    /// <summary>
    /// Tracked, because the caller is normally about to close the day off on the record that
    /// comes back rather than open a second one.
    /// </summary>
    public async Task<HumanAttendanceRecord?> GetForDayAsync(Guid humanId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(a => a.HumanId == humanId && a.Date == date, cancellationToken);
    }

    public async Task<IReadOnlyList<HumanAttendanceRecord>> GetBetweenAsync(Guid humanId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => a.HumanId == humanId && a.Date >= from && a.Date <= to)
            .OrderBy(a => a.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HumanAttendanceRecord>> GetForDateAsync(DateOnly date, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(a => a.Human)
            .Where(a => a.Date == date && (branchId == null || a.Human.BranchId == branchId))
            .OrderBy(a => a.Human.Family).ThenBy(a => a.Human.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(HumanAttendanceRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record, nameof(record));
        _entity.Add(record);
        return Task.CompletedTask;
    }
}
