using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Domain.Repositories.Modules.ProductionManagement;
/// <summary>
/// Material taken from the warehouse into a works order - مصرف مواد. The link that makes
/// production cost anything.
/// </summary>
public interface IMaterialIssueRepository : IRepository<MaterialIssue>
{
    Task<MaterialIssue?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    Task<MaterialIssue?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Everything issued to one order. This is what a works order actually consumed, as against
    /// what its recipe said it would, and the gap between the two is where material loss shows up.
    /// </summary>
    Task<IReadOnlyList<MaterialIssue>> GetByProductionOrderAsync(Guid productionOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// How much of a component has already been issued to an order, summed in the database. A
    /// second issue for the same shortage is the commonest way an order eats twice its materials.
    /// </summary>
    Task<decimal> GetIssuedQuantityAsync(Guid productionOrderId, Guid componentId, CancellationToken cancellationToken = default);
}
