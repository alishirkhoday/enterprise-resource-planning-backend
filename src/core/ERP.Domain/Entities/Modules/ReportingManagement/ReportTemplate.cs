namespace ERP.Domain.Entities.Modules.ReportingManagement;

/// <summary>
/// A report definition - name, category, filters and the query to run.
/// 
/// The query is stored, never executed by this application. An endpoint that ran a stored
/// query string would be remote code execution against the database however it was labelled;
/// running these belongs to a reporting service with its own read-only credentials.
/// </summary>
public sealed class ReportTemplate : AggregateRoot
{
    public string Name { get; private set; } = default!; // نام گزارش
    public string Description { get; private set; } = default!; // توضیح گزارش
    public ReportCategory Category { get; private set; } // دسته‌بندی (فروش، انبار، تولید، ...)
    public string QueryDefinition { get; private set; } = default!; // تعریف کوئری (SQL یا LINQ)
    public string OutputFormat { get; private set; } = "Table"; // نوع خروجی (Table, Chart, Pivot, Export)

    private readonly List<ReportFilterDefinition> _filters = new();
    public IReadOnlyList<ReportFilterDefinition> Filters => _filters.AsReadOnly();

    // Required by Entity Framework to materialise the entity.
    private ReportTemplate() { }

    public ReportTemplate(string name, string description, ReportCategory category, string queryDefinition, string outputFormat = "Table")
    {
        Name = name;
        Description = description;
        Category = category;
        QueryDefinition = queryDefinition;
        OutputFormat = outputFormat;
    }

    public void AddFilter(ReportFilterDefinition filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        EnsureCanAddChild(filter, filter.Template.Id, _filters);

        _filters.Add(filter);
    }
}
