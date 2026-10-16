using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.ProductionManagement;

public class MaterialIssueRepository(IMainDbContext context) : Repository<MaterialIssue>(context), IMaterialIssueRepository
{
    public async Task<MaterialIssue?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(i => i.Number == number, cancellationToken);
    }

    public async Task<MaterialIssue?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(i => i.ProductionOrder)
            .Include(i => i.Items).ThenInclude(line => line.Component)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialIssue>> GetByProductionOrderAsync(Guid productionOrderId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(i => i.Items).ThenInclude(line => line.Component)
            .Where(i => i.ProductionOrderId == productionOrderId && i.Status != MaterialIssueStatus.Cancelled)
            .OrderBy(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Only confirmed issues count. A draft has taken nothing off the shelf yet, and counting it
    /// would make an order look fed when the material is still in the warehouse.
    /// </summary>
    public async Task<decimal> GetIssuedQuantityAsync(Guid productionOrderId, Guid componentId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(i => i.ProductionOrderId == productionOrderId && i.Status == MaterialIssueStatus.Confirmed)
            .SelectMany(i => i.Items)
            .Where(line => line.ComponentId == componentId)
            .SumAsync(line => (decimal?)line.IssuedQuantity, cancellationToken) ?? 0m;
    }
}
