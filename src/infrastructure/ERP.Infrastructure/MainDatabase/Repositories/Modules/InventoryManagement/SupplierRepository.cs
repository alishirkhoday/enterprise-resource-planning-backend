using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class SupplierRepository(IMainDbContext context) : Repository<Supplier>(context), ISupplierRepository
{
    public async Task<Supplier?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(s => s.Code == code, cancellationToken);
    }

    public async Task<Supplier?> GetWithContactsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(s => s.Contacts)
            .Include(s => s.BankAccounts)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Supplier>> GetOrderableAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(s => s.Status == SupplierStatus.Active)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The preferred supplier first, then the rest by how well they have performed. A blacklisted
    /// or suspended supplier never appears, because purchasing must not propose one.
    /// </summary>
    public async Task<IReadOnlyList<ItemSupplier>> GetSuppliersForItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return await _context.ItemSuppliers
            .AsNoTracking()
            .Include(s => s.Supplier)
            .Where(s => s.ItemId == itemId
                        && s.IsActive
                        && s.Supplier.Status == SupplierStatus.Active)
            .OrderByDescending(s => s.IsPreferred)
            .ThenByDescending(s => s.Supplier.PerformanceScore ?? 0)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistenceByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(
            s => s.Code == code && (excludeId == null || s.Id != excludeId),
            cancellationToken);
    }
}
