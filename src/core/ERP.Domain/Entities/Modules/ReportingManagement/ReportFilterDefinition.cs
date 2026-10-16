namespace ERP.Domain.Entities.Modules.ReportingManagement;

/// <summary>
/// One input a report accepts, with the default it starts at.
/// </summary>
public sealed class ReportFilterDefinition : BaseEntity
{
    public ReportTemplate Template { get; private set; } = default!;
    public string FieldName { get; private set; } = default!;
    public FilterType Type { get; private set; }
    public string? DefaultValue { get; private set; }

    // Required by Entity Framework to materialise the entity.
    private ReportFilterDefinition() { }

    public ReportFilterDefinition(ReportTemplate template, string fieldName, FilterType type, string? defaultValue = null)
    {
        Template = template;
        FieldName = fieldName;
        Type = type;
        DefaultValue = defaultValue;
    }
}
