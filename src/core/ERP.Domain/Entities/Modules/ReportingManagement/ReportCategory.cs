namespace ERP.Domain.Entities.Modules.ReportingManagement;

/// <summary>
/// Which part of the business a report belongs to, for grouping them in a list.
/// </summary>
public enum ReportCategory
{
    Sales,
    Inventory,
    Production,
    Finance,
    HumanResources,
    CRM,
    Custom
}
