using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;
/// <summary>
/// Suppliers - تأمین‌کننده.
/// </summary>
public interface ISupplierRepository : IRepository<Supplier>
{
    Task<Supplier?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<Supplier?> GetWithContactsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Suppliers that may actually be ordered from today.</summary>
    Task<IReadOnlyList<Supplier>> GetOrderableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Who can supply an item and on what terms, best rated first. This is what turns a
    /// requisition into an order without anyone retyping a price.
    /// </summary>
    Task<IReadOnlyList<ItemSupplier>> GetSuppliersForItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<bool> IsExistenceByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
