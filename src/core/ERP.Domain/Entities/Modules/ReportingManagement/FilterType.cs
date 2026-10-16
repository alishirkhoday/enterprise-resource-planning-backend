namespace ERP.Domain.Entities.Modules.ReportingManagement;

/// <summary>
/// What kind of input a report filter takes, which decides how it is rendered and parsed.
/// </summary>
public enum FilterType
{
    Text,
    Number,
    Date,
    Enum,
    Boolean
}
