using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;
/// <summary>
/// Serial numbers - شماره سریال. For items tracked one unit at a time, where the history of
/// that exact unit is what matters.
/// </summary>
public interface ISerialNumberRepository : IRepository<SerialNumber>
{
    Task<SerialNumber?> GetByValueAsync(Guid itemId, string value, CancellationToken cancellationToken = default);

    /// <summary>Resolves a serial on its own, which is how a warranty claim usually arrives.</summary>
    Task<SerialNumber?> FindByValueAsync(string value, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SerialNumber>> GetInStockAsync(Guid itemId, Guid warehouseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SerialNumber>> GetSoldToAsync(Guid detailAccountId, CancellationToken cancellationToken = default);

    Task<bool> IsExistenceByValueAsync(Guid itemId, string value, CancellationToken cancellationToken = default);
}
