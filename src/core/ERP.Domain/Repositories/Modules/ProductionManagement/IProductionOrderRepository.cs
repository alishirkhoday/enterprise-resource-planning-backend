using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Domain.Repositories.Modules.ProductionManagement;
/// <summary>
/// Works orders - سفارش تولید. Also the way into their tasks, which have no repository of
/// their own because they have no life outside the order that scheduled them.
/// </summary>
public interface IProductionOrderRepository : IRepository<ProductionOrder>
{
    Task<ProductionOrder?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<ProductionOrder?> GetWithTasksAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionOrder>> GetByStatusAsync(ProductionOrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>Orders past their due date that have not finished.</summary>
    Task<IReadOnlyList<ProductionOrder>> GetLateAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// How much of an item is being made and not yet received into stock. Planning counts this as
    /// incoming supply alongside what is on order from suppliers.
    /// </summary>
    Task<decimal> GetQuantityInProductionAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// One task, found without knowing which order scheduled it. Tasks have no repository of
    /// their own because they have no life outside their order, but an inspection or a time
    /// booking arrives naming only the task.
    /// </summary>
    Task<ProductionTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Work scheduled onto a work centre in a window, which is how capacity is checked.</summary>
    Task<IReadOnlyList<ProductionTask>> GetScheduledTasksAsync(Guid workCenterId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    Task<string?> GetLastCodeAsync(CancellationToken cancellationToken = default);
}
