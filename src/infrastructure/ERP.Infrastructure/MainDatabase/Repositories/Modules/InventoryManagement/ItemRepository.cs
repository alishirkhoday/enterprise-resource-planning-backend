using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class ItemRepository(IMainDbContext context) : Repository<Item>(context), IItemRepository
{
    public async Task<Item?> GetByStockKeepingUnitAsync(string stockKeepingUnit, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(i => i.StockKeepingUnit == stockKeepingUnit, cancellationToken);
    }

    /// <summary>
    /// Looks in the barcode table as well as on the item itself, because an item carries several
    /// codes and a scanner has no idea which one it just read.
    /// </summary>
    public async Task<Item?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode, nameof(barcode));

        var byOwnCode = await _entity.FirstOrDefaultAsync(
            i => i.BarCode == barcode || i.QRCode == barcode,
            cancellationToken);

        if (byOwnCode is not null)
            return byOwnCode;

        var itemId = await _context.ItemBarcodes
            .AsNoTracking()
            .Where(b => b.Value == barcode)
            .Select(b => (Guid?)b.ItemId)
            .FirstOrDefaultAsync(cancellationToken);

        return itemId is null ? null : await GetByIdAsync(itemId.Value, cancellationToken);
    }

    public async Task<Item?> GetWithSpecificationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(i => i.Specifications).ThenInclude(s => s.Attribute)
            .Include(i => i.Specifications).ThenInclude(s => s.Value)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Item?> GetWithSuppliersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(i => i.Suppliers).ThenInclude(s => s.Supplier)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Item>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(i => i.GroupId == groupId && i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistenceByStockKeepingUnitAsync(string stockKeepingUnit, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(
            i => i.StockKeepingUnit == stockKeepingUnit && (excludeId == null || i.Id != excludeId),
            cancellationToken);
    }
}
