using ERP.Domain.Entities.Modules.ReportingManagement;

namespace ERP.Domain.Repositories.Modules.ReportingManagement;
/// <summary>
/// Dashboards and the widgets they are assembled from.
/// </summary>
public interface IDashboardRepository : IRepository<Dashboard>
{
    /// <summary>
    /// A dashboard with its widgets and the templates behind them, which is the only form in
    /// which it can be rendered.
    /// </summary>
    Task<Dashboard?> GetWithWidgetsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Dashboard?> GetByTitleAsync(string title, CancellationToken cancellationToken = default);
}
