namespace ERP.Domain.Entities.Modules.ReportingManagement;

/// <summary>
/// A screen assembled out of report templates.
/// </summary>
public sealed class Dashboard : AggregateRoot
{
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    private readonly List<DashboardWidget> _widgets = new();
    public IReadOnlyList<DashboardWidget> Widgets => _widgets.AsReadOnly();

    // Required by Entity Framework to materialise the entity.
    private Dashboard() { }

    public Dashboard(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public void AddWidget(DashboardWidget widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        EnsureCanAddChild(widget, widget.Dashboard.Id, _widgets);

        _widgets.Add(widget);
    }
}
