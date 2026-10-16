using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.ProductionManagement;

public class QualityControlCheckRepository(IMainDbContext context) : Repository<QualityControlCheck>(context), IQualityControlCheckRepository
{
    public async Task<QualityControlCheck?> GetWithMeasurementsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Inspector)
            .Include(c => c.Measurements).ThenInclude(m => m.Parameter)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<QualityControlCheck>> GetByProductionOrderAsync(Guid productionOrderId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Measurements).ThenInclude(m => m.Parameter)
            .Where(c => c.ProductionOrderId == productionOrderId)
            .OrderBy(c => c.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QualityControlCheck>> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Measurements).ThenInclude(m => m.Parameter)
            .Where(c => c.ReferenceType == referenceType && c.ReferenceId == referenceId)
            .OrderBy(c => c.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QualityControlCheck>> GetFailuresAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Measurements).ThenInclude(m => m.Parameter)
            .Where(c => !c.Passed && c.CheckedAt >= from && c.CheckedAt <= to)
            .OrderByDescending(c => c.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QualityParameter>> GetParametersAsync(IEnumerable<Guid> parameterIds, CancellationToken cancellationToken = default)
    {
        var ids = parameterIds.Distinct().ToArray();

        if (ids.Length == 0)
            return [];

        // Tracked, because a measurement holds a reference to its parameter and the two are saved
        // together; an untracked one would be inserted again as a copy.
        return await _context.SetEntity<QualityParameter>()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }
}
