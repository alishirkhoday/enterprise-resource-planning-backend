using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;
using ERP.Domain.Entities.Modules.ReportingManagement;
using ERP.Domain.Repositories.Modules.ReportingManagement;

namespace ERP.Application.UseCases.Modules.ReportingManagement.Commands.ComposeDashboard
{
    /// <summary>
    /// Assembles a dashboard out of report templates - داشبورد.
    /// <para>
    /// Every widget has to name a template that exists. A dashboard carrying a widget that points
    /// at nothing renders as a broken tile nobody can explain, and the moment to catch that is
    /// while it is being built.
    /// </para>
    /// </summary>
    public class ComposeDashboardCommandHandler(
        IDashboardRepository dashboards,
        IReportTemplateRepository templates)
        : IRequestHandler<ComposeDashboardCommand, Result<DashboardDto, Error>>
    {
        public async Task<Result<DashboardDto, Error>> Handle(ComposeDashboardCommand request, CancellationToken cancellationToken)
        {
            var title = request.Title.Trim();

            var existing = await dashboards.GetByTitleAsync(title, cancellationToken);
            if (existing is not null)
                return Result.Failure<DashboardDto, Error>(Errors.Reporting.DashboardTitleIsUsed(title));

            var found = await templates.GetByIdsAsync([.. request.Widgets.Select(w => w.ReportTemplateId)], cancellationToken);
            var byId = found.ToDictionary(t => t.Id);

            try
            {
                var dashboard = new Dashboard(title, request.Description);

                foreach (var widget in request.Widgets.OrderBy(w => w.DisplayOrder))
                {
                    if (!byId.TryGetValue(widget.ReportTemplateId, out var template))
                        return Result.Failure<DashboardDto, Error>(
                            Errors.General.NotFound(widget.ReportTemplateId.ToString(), nameof(ReportTemplate)));

                    dashboard.AddWidget(new DashboardWidget(dashboard, template, widget.DisplayOrder, widget.VisualizationType));
                }

                await dashboards.CreateAsync(dashboard, cancellationToken);

                return Result.Success<DashboardDto, Error>(ToDto(dashboard));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return Result.Failure<DashboardDto, Error>(Errors.Reporting.ReportCannotBeChanged(exception.Message));
            }
        }

        internal static DashboardDto ToDto(Dashboard dashboard) => new(
            dashboard.Id,
            dashboard.Title,
            dashboard.Description,
            [.. dashboard.Widgets
                .OrderBy(w => w.DisplayOrder)
                .Select(w => new DashboardWidgetViewDto(
                    w.Id,
                    w.ReportTemplate.Id,
                    w.ReportTemplate.Name,
                    w.DisplayOrder,
                    w.VisualizationType))]);
    }
}
