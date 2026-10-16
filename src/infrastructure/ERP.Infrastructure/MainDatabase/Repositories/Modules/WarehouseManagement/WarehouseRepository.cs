using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.WarehouseManagement;

public class WarehouseRepository(IMainDbContext context) : Repository<Warehouse>(context), IWarehouseRepository
{
    public async Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(w => w.Code == code, cancellationToken);
    }

    public async Task<Warehouse?> GetWithStorageLocationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(w => w.StorageLocations)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Warehouse>> GetByTypeAsync(WarehouseType type, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(w => w.Type == type)
            .OrderBy(w => w.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<WarehouseStorageLocation?> GetStorageLocationAsync(Guid warehouseId, string code, CancellationToken cancellationToken = default)
    {
        return await _context.WarehouseStorageLocations
            .FirstOrDefaultAsync(l => l.WarehouseId == warehouseId && l.Code == code, cancellationToken);
    }

    public async Task<bool> IsExistenceByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(
            w => w.Code == code && (excludeId == null || w.Id != excludeId),
            cancellationToken);
    }
}
