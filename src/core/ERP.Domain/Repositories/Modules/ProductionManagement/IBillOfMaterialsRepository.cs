using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Domain.Repositories.Modules.ProductionManagement;
/// <summary>
/// Recipes - BOM. Versioned and dated, because what a product was built from last year is not
/// what it is built from today, and an old works order has to stay costed against its own version.
/// </summary>
public interface IBillOfMaterialsRepository : IRepository<BillOfMaterials>
{
    /// <summary>
    /// The recipe to use for an item on a date: approved, default, and valid then. An order raised
    /// today and one raised last year may legitimately use different versions.
    /// </summary>
    Task<BillOfMaterials?> GetEffectiveForItemAsync(Guid itemId, DateOnly onDate, CancellationToken cancellationToken = default);

    Task<BillOfMaterials?> GetWithComponentsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BillOfMaterials>> GetVersionsForItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Everything an item is a component of. This answers the other direction: if a part is
    /// recalled or goes out of stock, what can no longer be built.
    /// </summary>
    Task<IReadOnlyList<BillOfMaterials>> GetUsingComponentAsync(Guid componentItemId, CancellationToken cancellationToken = default);
}
