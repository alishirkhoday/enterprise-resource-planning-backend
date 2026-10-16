using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.ProductionManagement;

public class BillOfMaterialsRepository(IMainDbContext context) : Repository<BillOfMaterials>(context), IBillOfMaterialsRepository
{
    /// <summary>
    /// Approved, valid on the date and marked as the default. Several versions can be valid at
    /// once during a changeover, so the default is what decides between them.
    /// </summary>
    public async Task<BillOfMaterials?> GetEffectiveForItemAsync(Guid itemId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(b => b.Components).ThenInclude(c => c.Component)
            .Where(b => b.ItemId == itemId
                        && b.Status == BillOfMaterialsStatus.Approved
                        && b.ValidityPeriod.StartDate <= onDate
                        && (b.ValidityPeriod.EndDate == null || b.ValidityPeriod.EndDate >= onDate))
            .OrderByDescending(b => b.IsDefault)
            .ThenByDescending(b => b.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BillOfMaterials?> GetWithComponentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(b => b.Item)
            .Include(b => b.Components).ThenInclude(c => c.Component)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BillOfMaterials>> GetVersionsForItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(b => b.ItemId == itemId)
            .OrderByDescending(b => b.Version)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The reverse question: what can no longer be built if this part is unavailable. Answered by
    /// searching the component lines rather than the headers.
    /// </summary>
    public async Task<IReadOnlyList<BillOfMaterials>> GetUsingComponentAsync(Guid componentItemId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(b => b.Item)
            .Where(b => b.Status == BillOfMaterialsStatus.Approved
                        && b.Components.Any(c => c.ComponentId == componentItemId))
            .ToListAsync(cancellationToken);
    }
}
