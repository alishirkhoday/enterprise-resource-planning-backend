namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Whether material has actually left the warehouse. A draft issue has taken nothing and
/// charged nothing.
/// </summary>
public enum MaterialIssueStatus
{
    Draft = 0,
    Confirmed = 1,
    Cancelled = 2
}
