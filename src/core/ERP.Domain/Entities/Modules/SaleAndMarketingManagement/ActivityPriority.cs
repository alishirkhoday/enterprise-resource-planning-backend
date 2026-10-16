namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// How urgent an activity is, which orders a salesperson's day alongside the due time.
/// </summary>
public enum ActivityPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
