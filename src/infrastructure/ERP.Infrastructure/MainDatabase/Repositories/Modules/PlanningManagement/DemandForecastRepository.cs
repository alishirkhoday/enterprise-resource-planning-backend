using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.PlanningManagement;
using ERP.Domain.Repositories.Modules.PlanningManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.PlanningManagement;

public class DemandForecastRepository(IMainDbContext context) : Repository<DemandForecast>(context), IDemandForecastRepository
{
    public async Task<DemandForecast?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(f => f.Code == code, cancellationToken);
    }

    public async Task<DemandForecast?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(f => f.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <summary>
    /// A warehouse-specific forecast wins over an organization-wide one, and the most recently
    /// approved wins between equals. Several can be valid at once during a changeover.
    /// </summary>
    public async Task<DemandForecast?> GetEffectiveAsync(DateOnly onDate, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(f => f.Lines)
            .Where(f => f.Status == DemandForecastStatus.Approved
                        && f.Horizon.StartDate <= onDate
                        && (f.Horizon.EndDate == null || f.Horizon.EndDate >= onDate)
                        && (f.WarehouseId == null || f.WarehouseId == warehouseId))
            .OrderByDescending(f => f.WarehouseId != null)
            .ThenByDescending(f => f.ApprovedOn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DemandForecast>> GetByStatusAsync(DemandForecastStatus status, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(f => f.Status == status)
            .OrderByDescending(f => f.Horizon.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DemandForecastLine>> GetLinesAwaitingActualsAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await _context.DemandForecastLines
            .AsNoTracking()
            .Include(l => l.Item)
            .Where(l => l.ActualQuantity == null
                        && l.BucketEndDate < asOf
                        && l.Forecast.Status != DemandForecastStatus.Draft)
            .OrderBy(l => l.BucketEndDate)
            .ToListAsync(cancellationToken);
    }
}
