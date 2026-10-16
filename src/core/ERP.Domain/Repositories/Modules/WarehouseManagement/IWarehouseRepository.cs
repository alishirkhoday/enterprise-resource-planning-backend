using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Domain.Repositories.Modules.WarehouseManagement;
/// <summary>
/// Warehouses and the locations inside them.
/// </summary>
public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<Warehouse?> GetWithStorageLocationsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Warehouse>> GetByTypeAsync(WarehouseType type, CancellationToken cancellationToken = default);

    Task<WarehouseStorageLocation?> GetStorageLocationAsync(Guid warehouseId, string code, CancellationToken cancellationToken = default);

    Task<bool> IsExistenceByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
