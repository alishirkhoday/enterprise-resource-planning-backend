namespace ERP.Domain.Entities.Modules.ReportingManagement;

/// <summary>
/// A screen assembled out of report templates.
/// </summary>
public sealed class Dashboard : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    private readonly List<DashboardWidget> _widgets = new();
    public IReadOnlyList<DashboardWidget> Widgets => _widgets;

    // Required by Entity Framework to materialise the entity.
    private Dashboard() { }

    public Dashboard(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public void AddWidget(DashboardWidget widget) => _widgets.Add(widget);
}
