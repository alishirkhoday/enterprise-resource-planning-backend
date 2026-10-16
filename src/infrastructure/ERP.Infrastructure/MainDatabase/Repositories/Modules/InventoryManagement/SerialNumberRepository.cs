using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class SerialNumberRepository(IMainDbContext context) : Repository<SerialNumber>(context), ISerialNumberRepository
{
    public async Task<SerialNumber?> GetByValueAsync(Guid itemId, string value, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(s => s.ItemId == itemId && s.Value == value, cancellationToken);
    }

    /// <summary>
    /// A warranty claim usually arrives as a serial and nothing else, so this resolves it without
    /// being told which item it belongs to.
    /// </summary>
    public async Task<SerialNumber?> FindByValueAsync(string value, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(s => s.Item)
            .FirstOrDefaultAsync(s => s.Value == value, cancellationToken);
    }

    public async Task<IReadOnlyList<SerialNumber>> GetInStockAsync(Guid itemId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Where(s => s.ItemId == itemId
                        && s.WarehouseId == warehouseId
                        && s.Status == SerialNumberStatus.InStock)
            .OrderBy(s => s.ReceivedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SerialNumber>> GetSoldToAsync(Guid detailAccountId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(s => s.Item)
            .Where(s => s.SoldToDetailAccountId == detailAccountId)
            .OrderByDescending(s => s.SoldOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistenceByValueAsync(Guid itemId, string value, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(s => s.ItemId == itemId && s.Value == value, cancellationToken);
    }
}
