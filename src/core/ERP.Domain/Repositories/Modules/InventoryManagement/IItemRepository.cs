using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;
/// <summary>
/// The item master - کالا. What can be bought, sold or made, and how each is tracked.
/// </summary>
public interface IItemRepository : IRepository<Item>
{
    Task<Item?> GetByStockKeepingUnitAsync(string stockKeepingUnit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves whatever was scanned. An item carries several codes, because a carton and a single
    /// unit scan differently, so the search covers the barcode table as well as the item itself.
    /// </summary>
    Task<Item?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>The item with the attribute values that distinguish it from its siblings.</summary>
    Task<Item?> GetWithSpecificationsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Item?> GetWithSuppliersAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<bool> IsExistenceByStockKeepingUnitAsync(string stockKeepingUnit, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
