using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Domain.Repositories.Modules.ProductionManagement;
/// <summary>
/// Inspections and the readings they took - کنترل کیفیت. Also the way to the parameters a
/// reading is judged against.
/// </summary>
public interface IQualityControlCheckRepository : IRepository<QualityControlCheck>
{
    Task<QualityControlCheck?> GetWithMeasurementsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QualityControlCheck>> GetByProductionOrderAsync(Guid productionOrderId, CancellationToken cancellationToken = default);

    /// <summary>What was inspected against one document, such as a receipt or a production run.</summary>
    Task<IReadOnlyList<QualityControlCheck>> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Failed inspections in a window. A rising count is the earliest signal a process has drifted,
    /// so it is queried rather than assembled from the whole history.
    /// </summary>
    Task<IReadOnlyList<QualityControlCheck>> GetFailuresAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>The parameters an inspection records readings against.</summary>
    Task<IReadOnlyList<QualityParameter>> GetParametersAsync(IEnumerable<Guid> parameterIds, CancellationToken cancellationToken = default);
}
