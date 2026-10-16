using ERP.Domain.Entities.Modules.PlanningManagement;

namespace ERP.Domain.Repositories.Modules.PlanningManagement;
/// <summary>
/// Demand forecasts - پیش‌بینی تقاضا. What planning runs against before any order exists.
/// </summary>
public interface IDemandForecastRepository : IRepository<DemandForecast>
{
    Task<DemandForecast?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<DemandForecast?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The approved forecast a planning run should read for a date: the one whose horizon covers
    /// it, most recently approved first when several do.
    /// </summary>
    Task<DemandForecast?> GetEffectiveAsync(DateOnly onDate, Guid? warehouseId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DemandForecast>> GetByStatusAsync(DemandForecastStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lines whose bucket has passed and where nobody has recorded what actually sold. Until they
    /// are filled in, no forecast method can be judged against reality.
    /// </summary>
    Task<IReadOnlyList<DemandForecastLine>> GetLinesAwaitingActualsAsync(DateOnly asOf, CancellationToken cancellationToken = default);
}
