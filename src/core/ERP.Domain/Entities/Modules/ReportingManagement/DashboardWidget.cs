namespace ERP.Domain.Entities.Modules.ReportingManagement;

/// <summary>
/// One tile on a dashboard: which report it draws and how.
/// </summary>
public sealed class DashboardWidget : BaseEntity
{
    public Dashboard Dashboard { get; private set; } = default!;
    public ReportTemplate ReportTemplate { get; private set; } = default!;
    public int DisplayOrder { get; private set; } // ترتیب نمایش
    public string VisualizationType { get; private set; } = "Table"; // نوع نمایش (Table, Chart, KPI)

    // Required by Entity Framework to materialise the entity.
    private DashboardWidget() { }

    public DashboardWidget(Dashboard dashboard, ReportTemplate reportTemplate, int displayOrder, string visualizationType = "Table")
    {
        Dashboard = dashboard;
        ReportTemplate = reportTemplate;
        DisplayOrder = displayOrder;
        VisualizationType = visualizationType;
    }
}
