using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.ProductionManagement;

public class MachineRepository(IMainDbContext context) : Repository<Machine>(context), IMachineRepository
{
    public async Task<Machine?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(m => m.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Machine>> GetByStatusAsync(MachineStatus status, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(m => m.Status == status)
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Machine>> GetByWorkCenterAsync(Guid workCenterId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(m => m.WorkCenterId == workCenterId)
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Machines with no interval set are excluded rather than treated as never due. An interval of
    /// nothing means nobody has decided how often it needs servicing, which is a data gap, not a
    /// machine in perfect health - reporting it as due would bury the real ones.
    /// </summary>
    public async Task<IReadOnlyList<Machine>> GetDueForMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(m => m.MaintenanceIntervalHours != null
                        && m.Status != MachineStatus.Retired
                        && m.RunningHours - m.RunningHoursAtLastService >= m.MaintenanceIntervalHours)
            .OrderByDescending(m => m.RunningHours - m.RunningHoursAtLastService)
            .ToListAsync(cancellationToken);
    }
}
